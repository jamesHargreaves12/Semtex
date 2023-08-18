using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SemtexAnalyzers;

public class CanBeMadeStaticCodeFixProvider: CodeFixProvider
{
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var root =  await context.Document.GetSyntaxRootAsync().ConfigureAwait(false);

        var node = root!.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        if (node is not MemberDeclarationSyntax memberDeclaration) return;
        var codeAction = CodeAction.Create(
            nameof(CanBeMadeStaticCodeFixProvider),
            ct =>
            {
                var staticModifier = SyntaxFactory.Token(SyntaxKind.StaticKeyword);
                var newMethodDeclaration = memberDeclaration.AddModifiers(staticModifier);

                var newRoot = root.ReplaceNode(node, newMethodDeclaration);
                return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
            },
            
            diagnostic.Id
        );    
        context.RegisterCodeFix(codeAction, diagnostic);
    }

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(new[] { DiagnosticDescriptors.CanBeMadeStaticId });
}