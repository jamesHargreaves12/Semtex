using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SemtexAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConstantValueAnalyzer: DiagnosticAnalyzer
{

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        // Probably more places can use this but for now this is fine.
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.AddExpression);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SubtractExpression);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MultiplyExpression);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.DivideExpression);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.IdentifierName);
    }

    private static void Analyze(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        var expression = syntaxNodeAnalysisContext.Node;
        
        // If the thing that is constant is a member access then we need to replace the whole thing.
        var parent = expression.Parent;
        while (parent is MemberAccessExpressionSyntax memberAccess)
        {
            parent = memberAccess.Parent;
        }
        
        // If in AnonymousType then we shouldn't change it as the name matters.
        if (parent is AnonymousObjectMemberDeclaratorSyntax)
        {
            return;
        }

        var semanticModel = syntaxNodeAnalysisContext.SemanticModel;
        var constantValue = semanticModel.GetConstantValue(expression);

        if (!constantValue.HasValue || constantValue.Value is null)
        {
            return;
        }

        var typeInfo = semanticModel.GetTypeInfo(expression);
        // IF we are going to change the type we should abort (enums => int can cause issue)
        if (typeInfo.ConvertedType!.Name != constantValue.Value.GetType().Name)
        {
            return;
        }

        var properties = ConstantValuePropertyDict.GetPropertiesDict(constantValue.Value!);
        // Need properties to be able to fix on other side.
        if (properties is null)
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            descriptor: DiagnosticDescriptors.ConstantValueDiagnosticDescriptors,
            location: expression.GetLocation(),
            properties: properties);
        
        syntaxNodeAnalysisContext.ReportDiagnostic(diagnostic);
    }


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
        get
        {
            return ImmutableArray.Create(new[]
                {
                    DiagnosticDescriptors.ConstantValueDiagnosticDescriptors
                }
            );
        }
    }
}