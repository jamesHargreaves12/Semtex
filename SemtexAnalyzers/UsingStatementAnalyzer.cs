using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SemtexAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UsingStatementAnalyzer: DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.UsingStatement);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var usingStatement = (UsingStatementSyntax)context.Node;

        var parentBlock = usingStatement.Parent as BlockSyntax;
        if (parentBlock == null)
            return;

        var lastStatement = parentBlock.Statements.LastOrDefault();
        if(lastStatement != usingStatement)
            return;
        
        if (usingStatement.Declaration is not VariableDeclarationSyntax )
        {
            return;
        }

        var diagnostic = Diagnostic.Create(DiagnosticDescriptors.UsingStatementDescriptor, usingStatement.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
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