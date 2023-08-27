using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Semtex.Rewriters;

// Note that there is an assumption here that this will only be called on a subset of the method for which the _mapping was setup.
// This enables us do less checks but this constraint needs to be more clear TODO.
public class LocalVariableRenameRewriter: CSharpSyntaxRewriter
{
    private readonly SemanticModel _semanticModel;
    private readonly Dictionary<string, string> _mapping;

    public LocalVariableRenameRewriter(List<(string, string)> renames, SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
        _mapping = renames.ToDictionary(x => x.Item1, x => x.Item2);
    }
    
    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (_mapping.ContainsKey(node.Identifier.ValueText) 
            && _semanticModel.GetSymbolInfo(node).Symbol is ILocalSymbol or IParameterSymbol or IRangeVariableSymbol)
            return node.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.LeadingTrivia, _mapping[node.Identifier.ValueText], node.Identifier.TrailingTrivia));

        return base.VisitIdentifierName(node);
    }

    public override SyntaxNode? VisitFromClause(FromClauseSyntax node)
    {
        return _mapping.ContainsKey(node.Identifier.ValueText) 
            ? node.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.LeadingTrivia, _mapping[node.Identifier.ValueText], node.Identifier.TrailingTrivia)) 
            : base.VisitFromClause(node);
    }
    
    public override SyntaxNode? VisitParameter(ParameterSyntax node)
    {
        return _mapping.ContainsKey(node.Identifier.ValueText) 
            ? node.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.LeadingTrivia, _mapping[node.Identifier.ValueText], node.Identifier.TrailingTrivia)) 
            : base.VisitParameter(node);
    }
    
    public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
        return _mapping.ContainsKey(node.Identifier.ValueText) 
            ? node.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.LeadingTrivia, _mapping[node.Identifier.ValueText], node.Identifier.TrailingTrivia)) 
            : base.VisitVariableDeclarator(node);
    }
    
    public override SyntaxNode? VisitSingleVariableDesignation(SingleVariableDesignationSyntax node)
    {
        return _mapping.ContainsKey(node.Identifier.ValueText) 
            ? node.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.LeadingTrivia, _mapping[node.Identifier.ValueText], node.Identifier.TrailingTrivia)) 
            : base.VisitSingleVariableDesignation(node);
    }

}