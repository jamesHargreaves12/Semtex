using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SemtexAnalyzers;

public class UsingStatementCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(new[] { DiagnosticDescriptors.UsingStatementId });

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var root = await context.Document.GetSyntaxRootAsync().ConfigureAwait(false);

        var node = root!.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        if (node is not UsingStatementSyntax usingStatement)
            return;

        if (usingStatement.Parent is not BlockSyntax parentBlock)
            return;

        if (usingStatement.Declaration is not VariableDeclarationSyntax usingVariableDeclaration)
            return;

        var codeAction = CodeAction.Create(
            nameof(UsingStatementCodeFixProvider),
            ct =>
            {
                var usingDeclarations = new List<LocalDeclarationStatementSyntax>()
                {
                    SyntaxFactory.LocalDeclarationStatement(declaration: usingVariableDeclaration)
                        .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword))
                };

                BlockSyntax block;
                while (true)
                {
                    if (usingStatement.Statement is BlockSyntax blockSyntax)
                    {
                        block = blockSyntax;
                        break;
                    }
                    if (usingStatement.Statement is not UsingStatementSyntax childUsingStatement || childUsingStatement.Declaration is not VariableDeclarationSyntax childUsingVariableDeclaration)
                    {
                        throw new InvalidOperationException();
                    }
                    usingDeclarations.Add(
                        SyntaxFactory.LocalDeclarationStatement(declaration: childUsingVariableDeclaration)
                            .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword))
                    );
                    usingStatement = childUsingStatement;
                }
                var newStatements = parentBlock.Statements.Take(parentBlock.Statements.Count - 1).Concat(usingDeclarations).Concat(block.Statements);
                var newParentBlock = parentBlock.WithStatements(new SyntaxList<StatementSyntax>(newStatements));
                var newRoot = root.ReplaceNode(parentBlock, newParentBlock);
                return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
            },

            diagnostic.Id
        );
        context.RegisterCodeFix(codeAction, diagnostic);

    }

    public override FixAllProvider? GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }
}