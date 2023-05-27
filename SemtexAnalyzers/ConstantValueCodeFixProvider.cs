using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SemtexAnalyzers;

public class ConstantValueCodeFixProvider: CodeFixProvider
{
    private static SyntaxNode GetNewNodeFromConstantValue(object constantValue)
    {
        return constantValue switch
        {
            string s => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(s)),
            int i and >= 0 => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(i)),
            // This is slightly awkward but the casting to an enum can't be done from a negative literal - https://learn.microsoft.com/en-us/dotnet/csharp/misc/cs0075.
            int i and < 0 => SyntaxFactory.ParenthesizedExpression(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(i))), 
            float f => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(f)),
            double d => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(d)),
            _ => throw new NotImplementedException()
        };
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        // var constantValue = diagnostic.
        var properties = diagnostic.Properties;
        var root =  await context.Document.GetSyntaxRootAsync().ConfigureAwait(false);

        var node = root!.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        // if constant is X.Name and "Name" is constant then we should replace X.Name
        if (node.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == node 
            || node.Parent is QualifiedNameSyntax qualifiedNameSyntax && qualifiedNameSyntax.Right == node )
        {
            node = node.Parent;
        }

        var codeAction = CodeAction.Create(
            nameof(ConstantValueCodeFixProvider),
            ct => {
                var value = ConstantValuePropertyDict.GetValueFromPropDict(properties);
                var newNode = GetNewNodeFromConstantValue(value);
                var newRoot = root.ReplaceNode(node, newNode);
                return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
            },
            diagnostic.Id
        );
        context.RegisterCodeFix(codeAction, diagnostic);
    }

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(new[] { DiagnosticDescriptors.ConstantValueId });
}