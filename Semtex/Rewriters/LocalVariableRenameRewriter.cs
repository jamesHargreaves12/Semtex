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

    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        if (!token.IsKind(SyntaxKind.IdentifierToken) || !_mapping.ContainsKey(token.ValueText))
            return base.VisitToken(token);
        
        switch (token.Parent)
        {
            case IdentifierNameSyntax identifierNameSyntax:
                var symbol = _semanticModel.GetSymbolInfo(identifierNameSyntax).Symbol;
                // Confirm that this is referencing a local variable as it could be a member access.
                if (symbol is ILocalSymbol or IParameterSymbol or IRangeVariableSymbol)
                {
                    return SyntaxFactory.Identifier(token.LeadingTrivia, _mapping[token.ValueText], token.TrailingTrivia);
                }
                break;
            case FromClauseSyntax:
                return SyntaxFactory.Identifier(token.LeadingTrivia, _mapping[token.ValueText], token.TrailingTrivia);
            case VariableDeclaratorSyntax:
            case ParameterSyntax: 
                return SyntaxFactory.Identifier(token.LeadingTrivia, _mapping[token.ValueText], token.TrailingTrivia);
        }
        return base.VisitToken(token);
    }
}