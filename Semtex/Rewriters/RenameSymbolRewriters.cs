using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Semtex.Rewriters;

public class RenameSymbolRewriter: CSharpSyntaxRewriter
{
    private readonly SemanticModel _semanticModel;
    private readonly Dictionary<string, (ISymbol oldSymbol, string newName)> _oldNameToOldSymbolNewName;

    public RenameSymbolRewriter(SemanticModel semanticModel, Dictionary<string, (ISymbol oldSymbol, string newName)> oldNameToOldSymbolNewName)
    {
        _semanticModel = semanticModel;
        _oldNameToOldSymbolNewName = oldNameToOldSymbolNewName;
    }
    
    public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (_oldNameToOldSymbolNewName.ContainsKey(node.Identifier.Text) 
            && _semanticModel.GetSymbolInfo(node).Symbol!.Equals(_oldNameToOldSymbolNewName[node.Identifier.Text].oldSymbol))
        {
            return node.WithIdentifier(SyntaxFactory.Identifier(_oldNameToOldSymbolNewName[node.Identifier.Text].newName));
        }
        return base.VisitIdentifierName(node);
    }

    public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (_oldNameToOldSymbolNewName.ContainsKey(node.Identifier.Text) 
            && _semanticModel.GetDeclaredSymbol(node)!.Equals(_oldNameToOldSymbolNewName[node.Identifier.Text].oldSymbol))
        {
            return node.WithIdentifier(SyntaxFactory.Identifier(_oldNameToOldSymbolNewName[node.Identifier.Text].newName));
        }
        return base.VisitPropertyDeclaration(node);
    }

    public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
    {
        if (_oldNameToOldSymbolNewName.ContainsKey(node.Identifier.Text) 
            && _semanticModel.GetDeclaredSymbol(node)!.Equals(_oldNameToOldSymbolNewName[node.Identifier.Text].oldSymbol))
        {
            return node.WithIdentifier(SyntaxFactory.Identifier(_oldNameToOldSymbolNewName[node.Identifier.Text].newName));
        }
        return base.VisitStructDeclaration(node);
    }
    
    public override SyntaxNode VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        if (_oldNameToOldSymbolNewName.ContainsKey(node.Identifier.Text) 
            && _semanticModel.GetDeclaredSymbol(node)!.Equals(_oldNameToOldSymbolNewName[node.Identifier.Text].oldSymbol))
        {
            return node.WithIdentifier(SyntaxFactory.Identifier(_oldNameToOldSymbolNewName[node.Identifier.Text].newName));
        }
        return base.VisitRecordDeclaration(node);
    }


    public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
        if (_oldNameToOldSymbolNewName.ContainsKey(node.Identifier.Text) 
            && _semanticModel.GetDeclaredSymbol(node)!.Equals(_oldNameToOldSymbolNewName[node.Identifier.Text].oldSymbol))
        {
            return node.WithIdentifier(SyntaxFactory.Identifier(_oldNameToOldSymbolNewName[node.Identifier.Text].newName));
        }
        return base.VisitVariableDeclarator(node);
    }

}