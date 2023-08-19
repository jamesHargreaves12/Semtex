using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Semtex.Rewriters;

public class RenameSymbolRewriter: CSharpSyntaxRewriter
{
    private readonly SemanticModel _semanticModel;
    private readonly HashSet<ISymbol> _oldSymbols;
    private readonly HashSet<string> _oldSymbolNames;

    public RenameSymbolRewriter(SemanticModel semanticModel, HashSet<ISymbol> symbols)
    {
        _semanticModel = semanticModel;
        _oldSymbolNames = new HashSet<string>();
        foreach (var s in symbols)
        {
            _oldSymbolNames.Add(s.Name);
        }

        _oldSymbols = symbols;
    }

    private static string GetNewName(ISymbol s)
    {
        // TODO there is no need to keep this as any resembelance to what was there before. We should probably just make it semtex_{i} or something
        return $"p_{s.Name.ToLower().Trim('_')}";
    }

    public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (!_oldSymbolNames.Contains(node.Identifier.Text))
            return base.VisitIdentifierName(node);

        var currentSymbol = _semanticModel.GetSymbolInfo(node).Symbol;
        if (currentSymbol == null)
            return base.VisitIdentifierName(node);

        var symbol = _oldSymbols.SingleOrDefault(s => currentSymbol.Equals(s));
        if (symbol is null)
            return base.VisitIdentifierName(node);
        
        return node.WithIdentifier(SyntaxFactory.Identifier(GetNewName(symbol)));
    }

    public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (!_oldSymbolNames.Contains(node.Identifier.Text))
            return base.VisitPropertyDeclaration(node);

        var currentSymbol = _semanticModel.GetSymbolInfo(node).Symbol;
        if (currentSymbol == null)
            return base.VisitPropertyDeclaration(node);

        var symbol = _oldSymbols.SingleOrDefault(s => currentSymbol.Equals(s));
        if (symbol is null)
            return base.VisitPropertyDeclaration(node);
        
        return node.WithIdentifier(SyntaxFactory.Identifier(GetNewName(symbol)));
    }

    public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
    {
        if (!_oldSymbolNames.Contains(node.Identifier.Text))
            return base.VisitStructDeclaration(node);

        var currentSymbol = _semanticModel.GetSymbolInfo(node).Symbol;
        if (currentSymbol == null)
            return base.VisitStructDeclaration(node);

        var symbol = _oldSymbols.SingleOrDefault(s => currentSymbol.Equals(s));
        if (symbol is null)
            return base.VisitStructDeclaration(node);
        
        return node.WithIdentifier(SyntaxFactory.Identifier(GetNewName(symbol)));
    }
    
    public override SyntaxNode VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        if (!_oldSymbolNames.Contains(node.Identifier.Text))
            return base.VisitRecordDeclaration(node);

        var currentSymbol = _semanticModel.GetSymbolInfo(node).Symbol;
        if (currentSymbol == null)
            return base.VisitRecordDeclaration(node);

        var symbol = _oldSymbols.SingleOrDefault(s => currentSymbol.Equals(s));
        if (symbol is null)
            return base.VisitRecordDeclaration(node);
        
        return node.WithIdentifier(SyntaxFactory.Identifier(GetNewName(symbol)));
    }


    public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
        if (!_oldSymbolNames.Contains(node.Identifier.Text))
            return base.VisitVariableDeclarator(node);

        var currentSymbol = _semanticModel.GetSymbolInfo(node).Symbol;
        if (currentSymbol == null)
            return base.VisitVariableDeclarator(node);

        var symbol = _oldSymbols.SingleOrDefault(s => currentSymbol.Equals(s));
        if (symbol is null)
            return base.VisitVariableDeclarator(node);
        
        return node.WithIdentifier(SyntaxFactory.Identifier(GetNewName(symbol)));
    }

}