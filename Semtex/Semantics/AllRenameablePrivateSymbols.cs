using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp;

namespace Semtex.Semantics;

public class AllRenameablePrivateSymbols : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;

    public AllRenameablePrivateSymbols(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public HashSet<ISymbol> PrivateSymbols { get; } = new(SymbolEqualityComparer.Default);

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (node.Modifiers.All(m =>
                !m.IsKind(SyntaxKind.PublicKeyword)
                && !m.IsKind(SyntaxKind.ProtectedKeyword)
                && !m.IsKind(SyntaxKind.InternalKeyword))
            && (node.Parent is ClassDeclarationSyntax cds && cds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                || node.Parent is StructDeclarationSyntax sds && sds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                || node.Parent is RecordDeclarationSyntax rds && rds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
            ))
        {
            var symbol = _semanticModel.GetDeclaredSymbol(node);
            if (symbol is not null)
                PrivateSymbols.Add(symbol);
        }

        base.VisitClassDeclaration(node);
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        if (node.Modifiers.All(m =>
                !m.IsKind(SyntaxKind.PublicKeyword)
                && !m.IsKind(SyntaxKind.ProtectedKeyword)
                && !m.IsKind(SyntaxKind.InternalKeyword))
            && (node.Parent is ClassDeclarationSyntax cds && cds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                || node.Parent is StructDeclarationSyntax sds && sds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                || node.Parent is RecordDeclarationSyntax rds && rds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword)))
            && node.Declaration.Variables is [var declaration])
        {

            var symbol = _semanticModel.GetDeclaredSymbol(declaration);
            if (symbol is not null)
                PrivateSymbols.Add(symbol);
        }

        base.VisitFieldDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))
            && node.FirstAncestor(SyntaxKind.ClassDeclaration) is ClassDeclarationSyntax cds && cds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword)))
        {
            var symbol = _semanticModel.GetDeclaredSymbol(node);
            if (symbol is not null)
                PrivateSymbols.Add(symbol);
        }

        base.VisitStructDeclaration(node);
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        if (node.Modifiers.All(m =>
                !m.IsKind(SyntaxKind.PublicKeyword)
                && !m.IsKind(SyntaxKind.ProtectedKeyword)
                && !m.IsKind(SyntaxKind.InternalKeyword))
            && (node.Parent is ClassDeclarationSyntax cds && cds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                || node.Parent is StructDeclarationSyntax sds && sds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                || node.Parent is RecordDeclarationSyntax rds && rds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
            ))
        {
            PrivateSymbols.Add(_semanticModel.GetDeclaredSymbol(node)!);
        }

        base.VisitRecordDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (node.Modifiers.All(m =>
                !m.IsKind(SyntaxKind.PublicKeyword)
                && !m.IsKind(SyntaxKind.ProtectedKeyword)
                && !m.IsKind(SyntaxKind.InternalKeyword))
            && (node.Parent is ClassDeclarationSyntax cds && cds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                || node.Parent is StructDeclarationSyntax sds && sds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                || node.Parent is RecordDeclarationSyntax rds && rds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
            ))
        {
            var symbol = _semanticModel.GetDeclaredSymbol(node);
            if (symbol is not null)
                PrivateSymbols.Add(symbol);
        }

        base.VisitPropertyDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (node.Modifiers.All(m =>
                !m.IsKind(SyntaxKind.PublicKeyword)
                && !m.IsKind(SyntaxKind.ProtectedKeyword)
                && !m.IsKind(SyntaxKind.InternalKeyword))
            && (node.Parent is ClassDeclarationSyntax cds && cds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                || node.Parent is StructDeclarationSyntax sds && sds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                || node.Parent is RecordDeclarationSyntax rds && rds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
            ))
        {
            var symbol = _semanticModel.GetDeclaredSymbol(node);
            if (symbol is not null)
                PrivateSymbols.Add(symbol);
        }

        base.VisitMethodDeclaration(node);
    }
}
