using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Semtex.Rewriters;

internal class RenameSymbolRewriter: CSharpSyntaxRewriter
{
    private readonly SemanticModel _semanticModel;
    private readonly Dictionary<ISymbol, string> _renameMapping;
    private readonly HashSet<ISymbol> _oldSymbols;
    private readonly HashSet<string> _oldSymbolNames;

    public RenameSymbolRewriter(SemanticModel semanticModel, Dictionary<ISymbol, string> renameMapping)
    {
        _semanticModel = semanticModel;
        _renameMapping = renameMapping;
        _oldSymbolNames = new HashSet<string>();
        foreach (var s in renameMapping)
        {
            _oldSymbolNames.Add(s.Key.Name);
        }

        _oldSymbols = renameMapping.Keys.ToHashSet(SymbolEqualityComparer.Default);
        
    }
    
    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (!_oldSymbolNames.Contains(node.Identifier.Text))
            return base.VisitIdentifierName(node);

        var currentSymbol = _semanticModel.GetSymbolInfo(node).Symbol;
        if (currentSymbol == null)
            return base.VisitIdentifierName(node);

        var symbol = _oldSymbols.SingleOrDefault(s => currentSymbol.Equals(s,SymbolEqualityComparer.Default));
        if (symbol is null)
            return base.VisitIdentifierName(node);
        
        return node.WithIdentifier(SyntaxFactory.Identifier(_renameMapping[symbol]));
    }

    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (!_oldSymbolNames.Contains(node.Identifier.Text))
            return base.VisitPropertyDeclaration(node);

        var currentSymbol = _semanticModel.GetDeclaredSymbol(node);
        if (currentSymbol == null)
            return base.VisitPropertyDeclaration(node);

        var symbol = _oldSymbols.SingleOrDefault(s => currentSymbol.Equals(s,SymbolEqualityComparer.Default));
        if (symbol is null)
            return base.VisitPropertyDeclaration(node);
        
        return node.WithIdentifier(SyntaxFactory.Identifier(_renameMapping[symbol]));
    }

    public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
    {
        if (!_oldSymbolNames.Contains(node.Identifier.Text))
            return base.VisitStructDeclaration(node);

        var currentSymbol = _semanticModel.GetDeclaredSymbol(node);
        if (currentSymbol == null)
            return base.VisitStructDeclaration(node);

        var symbol = _oldSymbols.SingleOrDefault(s => currentSymbol.Equals(s,SymbolEqualityComparer.Default));
        if (symbol is null)
            return base.VisitStructDeclaration(node);
        
        return node.WithIdentifier(SyntaxFactory.Identifier(_renameMapping[symbol]));
    }
    
    public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        if (!_oldSymbolNames.Contains(node.Identifier.Text))
            return base.VisitRecordDeclaration(node);

        var currentSymbol = _semanticModel.GetDeclaredSymbol(node);
        if (currentSymbol == null)
            return base.VisitRecordDeclaration(node);

        var symbol = _oldSymbols.SingleOrDefault(s => currentSymbol.Equals(s,SymbolEqualityComparer.Default));
        if (symbol is null)
            return base.VisitRecordDeclaration(node);
        
        return node.WithIdentifier(SyntaxFactory.Identifier(_renameMapping[symbol]));
    }


    public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
        if (!_oldSymbolNames.Contains(node.Identifier.Text))
            return base.VisitVariableDeclarator(node);

        var currentSymbol = _semanticModel.GetDeclaredSymbol(node);
        if (currentSymbol == null)
            return base.VisitVariableDeclarator(node);

        var symbol = _oldSymbols.SingleOrDefault(s => currentSymbol.Equals(s,SymbolEqualityComparer.Default));
        if (symbol is null)
            return base.VisitVariableDeclarator(node);
        
        return node.WithIdentifier(SyntaxFactory.Identifier(_renameMapping[symbol]));
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (!_oldSymbolNames.Contains(node.Identifier.Text))
            return base.VisitMethodDeclaration(node);

        var currentSymbol = _semanticModel.GetDeclaredSymbol(node);
        if (currentSymbol == null)
            return base.VisitMethodDeclaration(node);

        var symbol = _oldSymbols.SingleOrDefault(s => currentSymbol.Equals(s,SymbolEqualityComparer.Default));
        if (symbol is null)
            return base.VisitMethodDeclaration(node);
        
        return node.WithIdentifier(SyntaxFactory.Identifier(_renameMapping[symbol]));
    }
}