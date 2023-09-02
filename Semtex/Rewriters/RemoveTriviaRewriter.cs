using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Semtex.Rewriters;

public class RemoveTriviaRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? Visit(SyntaxNode? node)
    {
        if (node == null) return null;
        var restOfTree = base.Visit(node);
        return restOfTree.WithoutTrivia();
    }
    public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia) => default;
}