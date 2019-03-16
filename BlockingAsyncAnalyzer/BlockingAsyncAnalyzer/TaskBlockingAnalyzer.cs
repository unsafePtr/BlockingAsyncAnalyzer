// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
// This source code was taken from the roslyn-analyzers repository https://github.com/dotnet/roslyn-analyzers/blob/f85fdc4c3dd6551f4a50d4d2968510d9286b6fdd/src/Unfactored/AsyncPackage/AsyncPackage/BlockingAsyncAnalyzer.cs

// Copyright(c) Nicolai Zdravcov.
// Changed to work in any method type (previously worked only in async methods)
// Check returning type for MemberAccessExpressionSyntax to be Task
// Removed checking for Sleep because Task doesn't contain this definition
// The .GetResult() case moved to separate analyzer

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Runtime.CompilerServices;

namespace BlockingAsyncAnalyzer
{
    /// <summary>
    /// This analyzer checks to see if asynchronous and synchronous code is mixed. 
    /// This causes blocking and deadlocks. The analyzer will check when async 
    /// methods are used and then checks if synchronous code is used within the method.
    /// A codefix will then change that synchronous code to its asynchronous counterpart.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TaskBlockingAnalyzer : DiagnosticAnalyzer
    {
        internal const string DiagnosticId = "PBS001";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Don't Mix Blocking and Async",
            messageFormat: "Possible blocking code",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
        
        internal static bool IsTaskReturnType(ITypeSymbol symbol)
        {
            const string TaskTypeDisplayString = "System.Threading.Tasks.Task";
            if(symbol == null || !symbol.IsType)
            {
                return false;
            }

            INamedTypeSymbol namedSymbol = symbol as INamedTypeSymbol;
            if (namedSymbol == null)
            {
                return false;
            }

            return namedSymbol.ToDisplayString() == TaskTypeDisplayString || namedSymbol.BaseType?.ToDisplayString() == TaskTypeDisplayString;
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.SimpleMemberAccessExpression);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var memberAccessNode = (MemberAccessExpressionSyntax)context.Node;
            var method = context.SemanticModel.GetEnclosingSymbol(context.Node.SpanStart) as IMethodSymbol;

            if (method != null) /* && (IsTaskReturnType(method.ReturnType) || method.IsAsync)) */ // this will allow only Task return type or async filtering
            {
                var invokeMethod = context.SemanticModel.GetSymbolInfo(context.Node).Symbol as IMethodSymbol;

                var location = memberAccessNode.Name.GetLocation();

                var parentSymbolType = memberAccessNode.Parent == null ? null : context.SemanticModel.GetSymbolInfo(memberAccessNode.Parent).Symbol?.ContainingType;
                if (invokeMethod != null && !invokeMethod.IsExtensionMethod && IsTaskReturnType(parentSymbolType))
                {
                    // Checks if the Wait method is called within an async method then creates the diagnostic.
                    if (invokeMethod.OriginalDefinition.Name.Equals("Wait"))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, location));
                        return;
                    }

                    // Checks if the WaitAny method is called within an async method then creates the diagnostic.
                    if (invokeMethod.OriginalDefinition.Name.Equals("WaitAny"))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, location));
                        return;
                    }

                    // Checks if the WaitAll method is called within an async method then creates the diagnostic.
                    if (invokeMethod.OriginalDefinition.Name.Equals("WaitAll"))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, location));
                        return;
                    }
                }

                var property = context.SemanticModel.GetSymbolInfo(context.Node).Symbol as IPropertySymbol;

                // Checks if the Result property is called within an async method then creates the diagnostic.
                if (property != null && property.OriginalDefinition.Name.Equals("Result") && IsTaskReturnType(property.ContainingType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, location));
                    return;
                }
            }
        }
    }
}
