using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SemtexAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UsingStatementAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.UsingStatement);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        if (context.Compilation is not CSharpCompilation { LanguageVersion: >= LanguageVersion.CSharp8 })
            return;

        var usingStatement = (UsingStatementSyntax)context.Node;

        var parentBlock = usingStatement.Parent as BlockSyntax;
        if (parentBlock == null)
            return;

        var lastStatement = parentBlock.Statements.LastOrDefault();
        if (lastStatement != usingStatement)
            return;

        if (usingStatement.Declaration is null)
        {
            return;
        }

        var statements = usingStatement.Statement;
        while (statements is not BlockSyntax)
        {
            if (statements is not UsingStatementSyntax childUsingStatement || childUsingStatement.Declaration is not VariableDeclarationSyntax)
            {
                return;
            }
            statements = childUsingStatement.Statement;

        }

        if (DoDeclaredVariablesOverlapWithOuterScope(usingStatement, context.SemanticModel))
            return;

        var diagnostic = Diagnostic.Create(DiagnosticDescriptors.UsingStatementDescriptor, usingStatement.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    // https://github.com/JosefPihrt/Roslynator/blob/914b232d7a7916ae8bd36f1bd472f5e708c7fc33/src/Common/CSharp/Analysis/ReduceIfNesting/IfLocalVariableAnalysis.cs#L12
    private static bool DoDeclaredVariablesOverlapWithOuterScope(
        StatementSyntax usingStatement,
        SemanticModel semanticModel
    )
    {
        ImmutableArray<ISymbol> variablesDeclared = semanticModel.AnalyzeDataFlow(usingStatement)!
            .VariablesDeclared;

        if (variablesDeclared.IsEmpty)
            return false;
        var parentStatements = usingStatement.Parent switch
        {
            BlockSyntax b => b.Statements,
            SwitchSectionSyntax s => s.Statements,
            _ => throw new ArgumentOutOfRangeException(nameof(usingStatement))
        };
        foreach (StatementSyntax statement in parentStatements)
        {
            if (statement == usingStatement)
                continue;

            foreach (ISymbol parentVariable in semanticModel.AnalyzeDataFlow(statement)!.VariablesDeclared)
            {
                foreach (ISymbol variable in variablesDeclared)
                {
                    if (variable.Name == parentVariable.Name)
                        return true;
                }
            }
        }

        return false;
    }


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            return ImmutableArray.Create(new[]
                {
                    DiagnosticDescriptors.UsingStatementDescriptor
                }
            );
        }
    }
}