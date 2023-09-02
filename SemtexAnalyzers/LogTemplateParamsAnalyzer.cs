using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SemtexAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LogTemplateParamsAnalyzer : DiagnosticAnalyzer
{

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // Probably more places can use this but for now this is fine.
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static readonly List<string> LogMethods = new() { "LogInformation", "LogDebug", "LogWarn", "LogError" };

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var invocationExpression = (InvocationExpressionSyntax)context.Node;

        if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
            return;

        if (!LogMethods.Contains(memberAccessExpression.Name.GetText().ToString()))
            return;

        var logMethodClassInterfaces = context.SemanticModel.GetTypeInfo(memberAccessExpression.Expression).Type?.Interfaces;
        if (logMethodClassInterfaces is null)
            return;

        if (logMethodClassInterfaces.Value.Length == 1 && logMethodClassInterfaces.Value.First().ToString() == "Microsoft.Extensions.Logging.ILogger<T>")
            return;

        if (invocationExpression.ArgumentList.Arguments.First().Expression is not LiteralExpressionSyntax
                literalExpression
            || !literalExpression.IsKind(SyntaxKind.StringLiteralExpression))
            return;

        var text = literalExpression.Token.Text;

        var matches = Regex.Matches(text, @"\{[^}]+\}");

        if (matches.Count == 0 || matches.All(x => x.Value == "{X}"))
            return;

        var diagnostic = Diagnostic.Create(
            descriptor: DiagnosticDescriptors.LogTemplateParamsDiagnosticDescriptors,
            location: literalExpression.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(new[]
                {
                    DiagnosticDescriptors.LogTemplateParamsDiagnosticDescriptors
                }
            );
        }
    }
}