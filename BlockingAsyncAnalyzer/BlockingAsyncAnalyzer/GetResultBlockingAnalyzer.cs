using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using BlockingAsyncAnalyzer.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace BlockingAsyncAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class GetResultBlockingAnalyzer : DiagnosticAnalyzer
    {
        internal const string DiagnosticId = "PBS002";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId, 
            title: "Don't Mix Blocking and Async",
            messageFormat: "GetResult() called on awaiter may block. Use await instead",
            category: "Usage", 
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.SimpleMemberAccessExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            IMethodSymbol invokeMethod = context.SemanticModel.GetSymbolInfo(context.Node).Symbol as IMethodSymbol;
            if (invokeMethod != null && invokeMethod.OriginalDefinition.Name == WellKnownMemberNames.GetResult && context.Node.Parent != null)
            {
                SyntaxNode getAwaiterSyntax = context.Node.Parent.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .FirstOrDefault(e => e != null && e.Identifier.ValueText == WellKnownMemberNames.GetAwaiter);
                
                // TODO: check case
                // var awaiter = taskLikeType.GetAwaiter();
                // var result = awaiter.GetResult()
                //if (getAwaiterSyntax == null)
                //{
                //    
                //}

                if (getAwaiterSyntax != null)
                {
                    IMethodSymbol getAwaiter = context.SemanticModel.GetSymbolInfo(getAwaiterSyntax).Symbol as IMethodSymbol;
                    if (getAwaiter != null && getAwaiter.IsValidGetAwaiter())
                    {
                        Location getResultLocation = context.Node.GetLocation();
                        Location getAwaiterLocation = getAwaiterSyntax.GetLocation();

                        Location location = Location.Create(
                            syntaxTree: context.Node.SyntaxTree, 
                            textSpan: TextSpan.FromBounds(getAwaiterLocation.SourceSpan.Start, getResultLocation.SourceSpan.End)
                        );

                        context.ReportDiagnostic(Diagnostic.Create(Rule, location));
                    }
                }
            }
        }
    }
}
