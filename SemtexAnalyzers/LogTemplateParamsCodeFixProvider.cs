using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SemtexAnalyzers;

public class LogTemplateParamsCodeFixProvider: CodeFixProvider
{
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];

        var root =  await context.Document.GetSyntaxRootAsync().ConfigureAwait(false);

        var node = root!.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        
        if (node is not LiteralExpressionSyntax literalExpression)
            return;
        
        var srcText = (string)literalExpression.Token.Value!;
        var newText = Regex.Replace(srcText, @"\{[^}]+\}", "{X}");
        
        if(srcText == newText)
            return;    
    
        
        var codeAction = CodeAction.Create(
            nameof(LogTemplateParamsCodeFixProvider),
             ct =>
             {
                 var newNode = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(newText));
                 var newRoot = root.ReplaceNode(node, newNode);
                 return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
             },
             diagnostic.Id
         );
         context.RegisterCodeFix(codeAction, diagnostic);
    }

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(new[] { DiagnosticDescriptors.LogTemplateParamsId });
}