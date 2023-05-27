using Microsoft.CodeAnalysis;

namespace SemtexAnalyzers;

public static class DiagnosticDescriptors
{
    private static string prefix = "semtex";

    public static string ConstantValueId = $"{prefix}_{nameof(ConstantValueDiagnosticDescriptors)}";
    public static DiagnosticDescriptor ConstantValueDiagnosticDescriptors = new(
        ConstantValueId,
        nameof(ConstantValueDiagnosticDescriptors),
        "messageFormat",
        "category",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

}