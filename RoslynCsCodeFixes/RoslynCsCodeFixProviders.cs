using System.Reflection;
using Microsoft.CodeAnalysis.CodeFixes;

namespace RoslynCsCodeFixes;

// Yeah... this is all very brittle but the types are all internal and so to use them requires accessing them
// via reflection which sucks. We have a test that confirms that the initialization of SupportingCodeFixes doesn't fail
// So we should be confident that things work but that doesn't solve the issue that version upgrades could be difficult
// if roslyn changes the classes / interfaces / etc (which they are perfectly within their right to do). Hopefully, 
// Roslyn is fairly stable at this point.
public static class RoslynCsCodeFixProviders
{
    // There is not a single public type in this assembly so we are going to have to just hard code this string.
    private static readonly Assembly CsharpCodeFixProviderAssembly = Assembly.Load(
        "Microsoft.CodeAnalysis.CSharp.Features, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

    public static CodeFixProvider GetRoslynCodeFixProvider(string typeFullName)
    {
        var t = CsharpCodeFixProviderAssembly.GetType(typeFullName);
        return (CodeFixProvider)t!.GetConstructor(new Type[]{})!.Invoke(null);
    }

    private static string GetEquivalenceKeyForCSharpRemoveUnnecessaryImportsCodeFixProvider(CodeFixProvider codeFixProvider)
    {
        // Yep Ew but this at leaves gives the future developer some breadcrums rather than a magic string.
        return (string)codeFixProvider
            .GetType()
            .GetMethod("GetTitle", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(codeFixProvider, new object?[]{})!;
    }

    public static Dictionary<string, CodeFixProvider> SupportedCodeFixes =
        new()
        {
            ["CS0219"]= GetRoslynCodeFixProvider("Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable.CSharpRemoveUnusedVariableCodeFixProvider"),
            ["CS0168"]= GetRoslynCodeFixProvider("Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable.CSharpRemoveUnusedVariableCodeFixProvider"),
            ["CS8019"]= GetRoslynCodeFixProvider("Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports.CSharpRemoveUnnecessaryImportsCodeFixProvider")
        };

    public static Dictionary<string, string?> SupportedCodeFixesEquivalentKeys =
        new()
        {
            ["CS8019"] = GetEquivalenceKeyForCSharpRemoveUnnecessaryImportsCodeFixProvider(
                GetRoslynCodeFixProvider(
                    "Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports.CSharpRemoveUnnecessaryImportsCodeFixProvider")
            ),
            // I believe that both of these should be `Remove_unused_variable` due to
            // https://github.com/dotnet/roslyn/blob/2dd36714034d09c4d339a2e4d95a32f6a78f18ec/src/Features/Core/Portable/RemoveUnusedVariable/AbstractRemoveUnusedVariableCodeFixProvider.cs#L55 .
            // However, since this provides its own batch code fix provider I think this would be unnecessary so not going to do that for now.
            ["CS0168"] = "CS0168",
            ["CS0219"] = "CS0219",
        };
}