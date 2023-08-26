using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SemtexAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CanBeMadeStaticAnalyzer: DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }
    
    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
     
        if(methodDeclaration.ExplicitInterfaceSpecifier is not null)
            return;
        
        if(methodDeclaration.Parent is InterfaceDeclarationSyntax)
            return;

        // Check if method is already static or non private
        if (methodDeclaration.Modifiers.Any(m =>
                m.IsKind(SyntaxKind.StaticKeyword) 
                || m.IsKind(SyntaxKind.PublicKeyword)
                || m.IsKind(SyntaxKind.InternalKeyword) 
                || m.IsKind(SyntaxKind.ProtectedKeyword)
                || m.IsKind(SyntaxKind.PartialKeyword)
                || m.IsKind(SyntaxKind.VirtualKeyword)
                || m.IsKind(SyntaxKind.SealedKeyword)
                || m.IsKind(SyntaxKind.OverrideKeyword)
                ))
        {
            return;
        }

        var semanticModel = context.SemanticModel;

        // Check if method accesses any instance members

        if (methodDeclaration.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Any(m => IsInstanceMemberAccess(m, semanticModel))) 
            return;

        if (methodDeclaration.DescendantNodes()
            .OfType<GenericNameSyntax>()
            .Any(m => IsInstanceMemberAccess(m, semanticModel))) 
            return;

        if (methodDeclaration.DescendantNodes().OfType<BaseExpressionSyntax>().Any())
            return;
        
        var symbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
        if (symbol is null || IsAccessedWithThisQualifier(symbol, semanticModel)) return;
        
        var diagnostic = Diagnostic.Create(DiagnosticDescriptors.CanBeMadeStaticDescriptor, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.Text);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsInstanceMemberAccess(ExpressionSyntax memberAccess, SemanticModel semanticModel)
    {
        // if accessing x.Y where x is not this then you are still fine to be static
        if (memberAccess.Parent is MemberAccessExpressionSyntax memberAccessExpression
            && memberAccessExpression.Name == memberAccess
            && memberAccessExpression.Expression is not ThisExpressionSyntax)
            return false;
        
        var symbol = semanticModel.GetSymbolInfo(memberAccess).Symbol;
        return symbol switch
        {
            IFieldSymbol fieldSymbol => !fieldSymbol.IsStatic,
            IMethodSymbol methodSymbol => !methodSymbol.IsStatic,
            IPropertySymbol propertySymbol => !propertySymbol.IsStatic,
            IEventSymbol eventSymbol => !eventSymbol.IsStatic,
            _ => false
        };
    }

    private static bool IsAccessedWithThisQualifier(ISymbol symbol, SemanticModel semanticModel)
    {
        var root = semanticModel.SyntaxTree.GetRoot();
        return root.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Any(identifier =>
            {
                if (identifier.Identifier.Text != symbol.Name) return false;

                if (identifier.Parent is not MemberAccessExpressionSyntax memberAccess
                    || memberAccess.Expression is not ThisExpressionSyntax)  // I think this really should be contains rather than just checking the top level but this is probably good enough.
                    return false;
                
                var invokedMethod = semanticModel.GetSymbolInfo(identifier).Symbol as IMethodSymbol;
                return invokedMethod != null && invokedMethod.Equals(symbol);
            });
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
        get
        {
            return ImmutableArray.Create(new[]
                {
                    DiagnosticDescriptors.CanBeMadeStaticDescriptor
                }
            );
        }
    }
}