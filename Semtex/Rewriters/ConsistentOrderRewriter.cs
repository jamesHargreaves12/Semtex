using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Semtex.Rewriters;

public class ConsistentOrderRewriter: CSharpSyntaxRewriter
{
    public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var rest = base.VisitClassDeclaration(node) as ClassDeclarationSyntax;
        // Inefficient but works.
        // Also a terrible order and we would almost certainly not want but all we really care about here is consistency.
        var reorderedMethods = new SyntaxList<MemberDeclarationSyntax>(rest!.Members.OrderByDescending(m=> m.ToFullString()));
        return SyntaxFactory.ClassDeclaration(rest.AttributeLists, rest.Modifiers, rest.Keyword, rest.Identifier,
            rest.TypeParameterList, rest.BaseList, rest.ConstraintClauses, rest.OpenBraceToken, reorderedMethods,
            rest.CloseBraceToken, rest.SemicolonToken);
    }
}