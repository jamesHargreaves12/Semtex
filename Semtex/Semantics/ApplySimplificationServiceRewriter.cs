using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp;

namespace Semtex.Semantics;

public class ApplySimplificationServiceRewriter : CSharpSyntaxRewriter
{
    private static List<SyntaxKind> _ignoreParentKind = new List<SyntaxKind>
    {
        SyntaxKind.QualifiedName,
        SyntaxKind.NamespaceDeclaration,
        SyntaxKind.FileScopedNamespaceDeclaration
    };

    public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
    {
        if (_ignoreParentKind.Contains(node.Parent!.Kind()))
            return base.VisitQualifiedName(node);
        
        return node.WithSimplifierAnnotation();
    }
}