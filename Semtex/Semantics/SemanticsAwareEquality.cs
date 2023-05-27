using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Semtex.Logging;

namespace Semtex.Semantics;

internal class SemanticsAwareEquality
{
    private static readonly ILogger<SemanticsAwareEquality> Logger = SemtexLog.LoggerFactory.CreateLogger<SemanticsAwareEquality>();
    internal static async Task<bool> SemanticallyEqual(Document left, Document right)
    {
        Logger.LogInformation("Checking Semantic Equality for {LeftFilePath}", left.FilePath);
        var leftTree = await left.GetSyntaxTreeAsync().ConfigureAwait(false);
        var rightTree = await right.GetSyntaxTreeAsync().ConfigureAwait(false);
        var leftRoot = await leftTree!.GetRootAsync().ConfigureAwait(false);
        var rightRoot = await rightTree!.GetRootAsync().ConfigureAwait(false);

        // return leftRoot.ToString() == rightRoot.ToString();
        Logger.LogInformation("Getting left semantic model");
        var leftCompilation = await left.Project.GetCompilationAsync().ConfigureAwait(false);
        var leftSemanticModel = leftCompilation!.GetSemanticModel(leftTree!);
        Logger.LogInformation("Getting right semantic model");
        var rightCompilation = await right.Project.GetCompilationAsync().ConfigureAwait(false);
        var rightSemanticModel = rightCompilation!.GetSemanticModel(rightTree!);
        Logger.LogInformation("Evaluating Equality");
        return await SemanticallyEqual(leftRoot, rightRoot, leftSemanticModel, rightSemanticModel, left, right).ConfigureAwait(false); // We can just push down the semantic model getting now that we are passing down the document.
    }

    // The idea here is that we can apply semantic equality bit by bit always falling back to a string comparison.
    private static async Task<bool> SemanticallyEqual(SyntaxNode left, SyntaxNode right, SemanticModel leftSemanticModel, SemanticModel rightSemanticModel, Document leftDocument, Document rightDocument)
    {
        return (left, right) switch
        {
            (CompilationUnitSyntax l, CompilationUnitSyntax r) => await SemanticallyEqualCompilationUnit(l, r, leftSemanticModel, rightSemanticModel, leftDocument, rightDocument).ConfigureAwait(false),
            (BaseNamespaceDeclarationSyntax l, BaseNamespaceDeclarationSyntax r) => await SemanticallyEqualNamespace(l, r, leftSemanticModel, rightSemanticModel, leftDocument, rightDocument).ConfigureAwait(false),
            (ClassDeclarationSyntax l, ClassDeclarationSyntax r) => await SemanticallyEqualClassDeclaration(l, r, leftSemanticModel, rightSemanticModel, leftDocument, rightDocument).ConfigureAwait(false),
            (MethodDeclarationSyntax l, MethodDeclarationSyntax r) => await SemanticallyEqualMethodDeclaration(l, r, leftSemanticModel, rightSemanticModel, leftDocument, rightDocument).ConfigureAwait(false),
            _ => left.ToString() == right.ToString()
        };
    }
    
    private static bool SemanticallyEqualSyntaxList<T>(SyntaxList<T> left, SyntaxList<T> right) where T : SyntaxNode
    {
        return left.ToString() == right.ToString();
    }

    private static bool SemanticallyEqualSyntaxTokenList(SyntaxTokenList left, SyntaxTokenList right)
    {
        return left.ToString() == right.ToString();
    }
    private static bool SemanticallyEqualBaseList(BaseListSyntax? left, BaseListSyntax? right)
    {
        // If both null return true if one is null return false
        if (left == null)
        {
            return right == null;
        }

        if (right == null)
        {
            return false;
        }

        return left.ToString() == right.ToString();
    }

    private static async Task<bool> SemanticallyEqualCompilationUnit(CompilationUnitSyntax left, CompilationUnitSyntax right, SemanticModel leftSemanticModel, SemanticModel rightSemanticModel, Document leftDocument, Document rightDocument)
    {
        return SemanticallyEqualUsings(left.Usings, right.Usings) &&
               SemanticallyEqualSyntaxList(left.Externs, right.Externs) &&
               SemanticallyEqualSyntaxList(left.AttributeLists, right.AttributeLists) &&
               await SemanticallyEqualMembers(left.Members, right.Members, leftSemanticModel, rightSemanticModel, leftDocument, rightDocument).ConfigureAwait(false);
    }

    private static async Task<bool> SemanticallyEqualNamespace(BaseNamespaceDeclarationSyntax left, BaseNamespaceDeclarationSyntax right, SemanticModel leftSemanticModel, SemanticModel rightSemanticModel, Document leftDocument, Document rightDocument)
    {
        return SemanticallyEqualSyntaxList(left.Externs, right.Externs) &&
               SemanticallyEqualUsings(left.Usings, right.Usings) &&
               await SemanticallyEqualMembers(left.Members, right.Members, leftSemanticModel, rightSemanticModel, leftDocument, rightDocument).ConfigureAwait(false) &&
               SemanticallyEqualSyntaxTokenList(left.Modifiers, right.Modifiers) &&
               SemanticallyEqualSyntaxList(left.AttributeLists, right.AttributeLists) &&
               left.Name.ToString() == right.Name.ToString();
    }
    
    private static async Task<bool> SemanticallyEqualClassDeclaration(ClassDeclarationSyntax left, ClassDeclarationSyntax right, SemanticModel leftSemanticModel, SemanticModel rightSemanticModel, Document leftDocument, Document rightDocument)
    {
        return SemanticallyEqualSyntaxList(left.AttributeLists, right.AttributeLists) &&
               SemanticallyEqualBaseList(left.BaseList, right.BaseList) &&
               SemanticallyEqualSyntaxList(left.ConstraintClauses, right.ConstraintClauses) && 
               left.Identifier.IsEquivalentTo(right.Identifier) &&
               NullableStringEqual(left.TypeParameterList, right.TypeParameterList) &&
               SemanticallyEqualSyntaxTokenList(left.Modifiers, right.Modifiers) &&
               await SemanticallyEqualMembers(left.Members, right.Members, leftSemanticModel, rightSemanticModel, leftDocument, rightDocument).ConfigureAwait(false);
    }

    private static async Task<bool> SemanticallyEqualMethodDeclaration(MethodDeclarationSyntax left, MethodDeclarationSyntax right, SemanticModel leftSemanticModel, SemanticModel rightSemanticModel,Document leftDocument, Document rightDocument)
    {
        if (!SemanticallyEqualSyntaxList(left.AttributeLists, right.AttributeLists) ||
            !SemanticallyEqualSyntaxList(left.ConstraintClauses, right.ConstraintClauses) ||
            !NullableStringEqual(left.ExplicitInterfaceSpecifier, right.ExplicitInterfaceSpecifier) ||
            !NullableStringEqual(left.ExpressionBody, right.ExpressionBody) ||
            !left.Identifier.IsEquivalentTo(right.Identifier) ||
            !SemanticallyEqualSyntaxTokenList(left.Modifiers, right.Modifiers) ||
            !StringEqual(left.ParameterList, right.ParameterList) ||
            !StringEqual(left.ReturnType, right.ReturnType) ||
            !NullableStringEqual(left.TypeParameterList, right.TypeParameterList) // Not checkint arity as it is covered by TypeParameterList
           )
            return false;
        // Check the body for equality
        if (left.Body == null)
        {
            return right.Body == null;
        }

        if (right.Body == null)
        {
            return false;
        }
        
        // Calculating Renames is more expensive so lets only do it for methods that are not equal before renaming:
        if (LocalStatementsEquality.SemanticallyEqualLocalStatements(left.Body.Statements, right.Body.Statements,
                leftSemanticModel, rightSemanticModel, new List<(string left, string right)>()))
        {
            return true;
        }

        var sw = Stopwatch.StartNew();
        var proposedRenames = await LocalVariableRenamer.GetProposedLocalVariableRenames(left, right, leftSemanticModel, rightSemanticModel,
            leftDocument, rightDocument).ConfigureAwait(false);
        Logger.LogInformation(SemtexLog.GetPerformanceStr(nameof(LocalVariableRenamer.GetProposedLocalVariableRenames), sw.ElapsedMilliseconds));

        return LocalStatementsEquality.SemanticallyEqualLocalStatements(left.Body.Statements,right.Body.Statements, leftSemanticModel, rightSemanticModel, proposedRenames);
    }
    
    private static bool SemanticallyEqualUsings(SyntaxList<UsingDirectiveSyntax> left, SyntaxList<UsingDirectiveSyntax> right)
    {        
        // Allow reordering of using directives.
        var leftUsings = left.Select(u => u.ToString()).ToHashSet();
        return leftUsings.SetEquals(right.Select(u => u.ToString()));
    }
    private static async Task<bool> SemanticallyEqualMembers(SyntaxList<MemberDeclarationSyntax> left, SyntaxList<MemberDeclarationSyntax> right, SemanticModel leftSemanticModel, SemanticModel rightSemanticModel, Document leftDocument, Document rightDocument)
    {
        // probably worth just making this into its own method - pairwise equality;
        if (left.Count != right.Count)
            return false;

        foreach (var (lMem,rMem) in left.Zip(right))
        {
            if (!await SemanticallyEqual(lMem, rMem, leftSemanticModel, rightSemanticModel, leftDocument, rightDocument).ConfigureAwait(false))
            {
                return false;
            }
        }

        return true;
    }

    private static bool StringEqual(SyntaxNode left, SyntaxNode right)
    {
        return left.ToString() == right.ToString();
    }
    private static bool NullableStringEqual(SyntaxNode? left, SyntaxNode? right)
    {
        if (left == null)
        {
            return right == null;
        }

        if (right == null)
        {
            return false;
        }

        return left.ToString() == right.ToString();
    }
}