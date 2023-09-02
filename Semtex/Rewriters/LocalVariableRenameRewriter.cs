using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Semtex.Rewriters;

// Note that there is an assumption here that this will only be called on a subset of the method for which the _mapping was setup.
// This enables us do less checks but this constraint needs to be more clear. Using ISymbol to access the dictionary would be better.
public class LocalVariableRenameRewriter : CSharpSyntaxRewriter
{
    private readonly SemanticModel _semanticModel;
    private readonly Dictionary<ISymbol, string> _mapping;
    private readonly HashSet<string> leftNames;

    public LocalVariableRenameRewriter(List<(ISymbol, string)> renames, SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
        _mapping = renames.ToDictionary(x => x.Item1, x => x.Item2, SymbolEqualityComparer.Default);
        leftNames = renames.Select(x => x.Item1.Name).ToHashSet();
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (!leftNames.Contains(node.Identifier.ValueText))
            return base.VisitIdentifierName(node);

        var symbol = _semanticModel.GetSymbolInfo(node).Symbol;
        if (symbol is not (ILocalSymbol or IParameterSymbol or IRangeVariableSymbol) || !_mapping.ContainsKey(symbol))
            return base.VisitIdentifierName(node);

        return node.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.LeadingTrivia, _mapping[symbol], node.Identifier.TrailingTrivia));
    }

    public override SyntaxNode? VisitFromClause(FromClauseSyntax node)
    {
        if (!leftNames.Contains(node.Identifier.ValueText))
            return base.VisitFromClause(node);

        var symbol = _semanticModel.GetDeclaredSymbol(node);
        return symbol is not null && _mapping.TryGetValue(symbol, out var value)
            ? node.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.LeadingTrivia, value, node.Identifier.TrailingTrivia))
            : base.VisitFromClause(node);
    }

    public override SyntaxNode? VisitParameter(ParameterSyntax node)
    {
        if (!leftNames.Contains(node.Identifier.ValueText))
            base.VisitParameter(node);
        var symbol = _semanticModel.GetDeclaredSymbol(node);

        return symbol is not null && _mapping.TryGetValue(symbol, out var value)
            ? node.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.LeadingTrivia, value, node.Identifier.TrailingTrivia))
            : base.VisitParameter(node);
    }

    public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
        if (!leftNames.Contains(node.Identifier.ValueText))
            base.VisitVariableDeclarator(node);
        var symbol = _semanticModel.GetDeclaredSymbol(node);

        return symbol is not null && _mapping.TryGetValue(symbol, out var value)
            ? node.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.LeadingTrivia, value, node.Identifier.TrailingTrivia))
            : base.VisitVariableDeclarator(node);
    }

    public override SyntaxNode? VisitSingleVariableDesignation(SingleVariableDesignationSyntax node)
    {
        if (!leftNames.Contains(node.Identifier.ValueText))
            base.VisitSingleVariableDesignation(node);
        var symbol = _semanticModel.GetDeclaredSymbol(node);

        return symbol is not null && _mapping.TryGetValue(symbol, out var value)
            ? node.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.LeadingTrivia, value, node.Identifier.TrailingTrivia))
            : base.VisitSingleVariableDesignation(node);
    }

}