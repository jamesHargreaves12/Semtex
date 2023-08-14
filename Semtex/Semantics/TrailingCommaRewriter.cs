using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Semtex.Semantics;

internal class TrailingCommaRewriter : CSharpSyntaxRewriter
{
    public override SeparatedSyntaxList<TNode> VisitList<TNode>(SeparatedSyntaxList<TNode> list)
    {
        return list.SeparatorCount == list.Count ? SyntaxFactory.SeparatedList(list) : list;
    }
}