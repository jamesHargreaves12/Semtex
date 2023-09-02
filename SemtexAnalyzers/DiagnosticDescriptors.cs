using Microsoft.CodeAnalysis;

namespace SemtexAnalyzers;

public static class DiagnosticDescriptors
{
    private const string Prefix = "semtex";

    public static readonly string ConstantValueId = $"{Prefix}_{nameof(ConstantValueDiagnosticDescriptors)}";
    public static readonly string LogTemplateParamsId = $"{Prefix}_{nameof(LogTemplateParamsDiagnosticDescriptors)}";
    public static readonly string CanBeMadeStaticId = $"{Prefix}_{nameof(CanBeMadeStaticDescriptor)}";
    public static readonly string UsingStatementId = $"{Prefix}_{nameof(UsingStatementDescriptor)}";
    public static DiagnosticDescriptor ConstantValueDiagnosticDescriptors = new(
        ConstantValueId,
        nameof(ConstantValueDiagnosticDescriptors),
        "messageFormat",
        "category",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor LogTemplateParamsDiagnosticDescriptors = new(
        LogTemplateParamsId,
        nameof(LogTemplateParamsDiagnosticDescriptors),
        "messageFormat",
        "category",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor CanBeMadeStaticDescriptor = new(
        CanBeMadeStaticId,
        nameof(CanBeMadeStaticDescriptor),
        "messageFormat",
        "category",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    public static DiagnosticDescriptor UsingStatementDescriptor = new(
        UsingStatementId,
        nameof(CanBeMadeStaticDescriptor),
        "messageFormat",
        "category",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

}