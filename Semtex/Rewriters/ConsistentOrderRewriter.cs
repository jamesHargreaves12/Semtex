using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Semtex.Rewriters;

public class ConsistentOrderRewriter : CSharpSyntaxRewriter
{
    private static (int, string, string) Order(MemberDeclarationSyntax member)
    {
        // Inefficient but works.
        // Also a terrible order and we would almost certainly not want but all we really care about here is consistency.

        return member switch
        {
            ClassDeclarationSyntax c => (1, c.Identifier.ToString(), c.ToString()),
            MethodDeclarationSyntax m => (2, m.Identifier.ToString(), m.ToString()),
            FieldDeclarationSyntax f => (3, f.Declaration.Variables.First().Identifier.ToString() + f.Declaration.Type, f.ToString()),
            PropertyDeclarationSyntax p => (4, p.Identifier.ToString() + p.Type, p.ToString()),
            _ => (5, "UNKNOWN", member.ToString()),
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