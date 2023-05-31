using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Semtex.Rewriters;

public class RemoveSuppressNullableWarningRewriter : CSharpSyntaxRewriter 
{
    public override SyntaxNode? VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
    {
        if (node.IsKind(SyntaxKind.SuppressNullableWarningExpression))
        { 
            return node.Operand;
        }
        return base.VisitPostfixUnaryExpression(node);
    }
}