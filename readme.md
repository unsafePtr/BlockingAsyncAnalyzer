
### Prehistory
Once we run into the deadlock issue with AzureFunction. The instance was sometimes crashing with a very strange exception. My colleague [Dimitar Nikolov](https://www.linkedin.com/in/dimitar-nikolov-10117689/) assumed that probably there is a deadlock issue. Then we found accessing `.Result` property on `Task` instead of awaiting it. Which leads to [deadlock](https://blog.stephencleary.com/2012/07/dont-block-on-async-code.html).

### Solution
I come to a solution that probably there is an analyzer to handle such cases. Hopefully, I found one in the [Microsoft's repository](https://github.com/dotnet/roslyn-analyzers/blob/f85fdc4c3dd6551f4a50d4d2968510d9286b6fdd/src/Unfactored/AsyncPackage/AsyncPackage/BlockingAsyncAnalyzer.cs). Unfortunately, there were several problems. The analyzer wasn't checking if property/field/variable returns `Task` type and was checking only methods with `async` prefix declaration.

My changes in the analyzer are:
1. Changed to work in any method type (previously worked only in async methods)
2. Check returning type for `MemberAccessExpressionSyntax` to be `Task`
3. Removed checking for `Sleep()` because `Task` doesn't contain this definition
4. The `.GetResult()` case moved to separate analyzer. Because C# evolves and we may have different types to *await*.

My skills weren't enough to update and migrate code fixing.

### Notes
Roslyn-analyzers repository [deleted some set of analyzers](https://github.com/dotnet/roslyn-analyzers/commit/9f3b2f47ca681c72df84b815e97b6e674cc5d34a) which include this too. They advised to use threading analyzers from the [vs-threading repository](https://github.com/Microsoft/vs-threading/tree/master/src/Microsoft.VisualStudio.Threading.Analyzers).