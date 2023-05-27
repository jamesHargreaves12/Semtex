using Microsoft.CodeAnalysis.CodeFixes;

namespace SemtexAnalyzers;

public class SemtexCodeFixProviders
{
    public static Dictionary<string, CodeFixProvider> SupportedCodeFixes =
        new Dictionary<string, CodeFixProvider>()
        {
            [DiagnosticDescriptors.ConstantValueId] = new ConstantValueCodeFixProvider()
        };

}