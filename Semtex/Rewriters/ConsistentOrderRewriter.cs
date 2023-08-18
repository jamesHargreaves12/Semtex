using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Semtex.Rewriters;

public class ConsistentOrderRewriter: CSharpSyntaxRewriter
{
    private static string Order(MemberDeclarationSyntax member)
    {
        // Inefficient but works.
        // Also a terrible order and we would almost certainly not want but all we really care about here is consistency.

        return member switch
        {
            ClassDeclarationSyntax c => "1" + c.Identifier + c,
            MethodDeclarationSyntax m => "2" + m.Identifier + m,
            _ => "3" + member
        };
    }

    public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var rest = base.VisitClassDeclaration(node) as ClassDeclarationSyntax;
        var reorderedMethods = new SyntaxList<MemberDeclarationSyntax>(rest!.Members.OrderByDescending(Order));
        return SyntaxFactory.ClassDeclaration(rest.AttributeLists, rest.Modifiers, rest.Keyword, rest.Identifier,
            rest.TypeParameterList, rest.BaseList, rest.ConstraintClauses, rest.OpenBraceToken, reorderedMethods,
            rest.CloseBraceToken, rest.SemicolonToken);
    }
}