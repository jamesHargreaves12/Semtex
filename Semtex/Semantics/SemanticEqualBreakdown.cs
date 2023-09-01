using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using OneOf;
using Semtex.Logging;
using Semtex.Models;

namespace Semtex.Semantics;

public struct CouldntLimitToFunctions
{
}

public struct DifferencesLimitedToFunctions
{
    //EmptyList => no differences.
    public List<MethodIdentifier> MethodIdentifiers { get; }

    public DifferencesLimitedToFunctions(List<MethodIdentifier> methodIdentifiers)
    {
        MethodIdentifiers = methodIdentifiers;
    }
    public DifferencesLimitedToFunctions()
    {
        MethodIdentifiers = new List<MethodIdentifier>();
    }
}

public sealed class SemanticEqualBreakdown
{
    private static readonly ILogger<SemanticEqualBreakdown> Logger = SemtexLog.LoggerFactory.CreateLogger<SemanticEqualBreakdown>();


    internal static async Task<OneOf<DifferencesLimitedToFunctions, CouldntLimitToFunctions>> GetSemanticallyUnequal(Document left, Document right)
    {
        Logger.LogDebug("Checking Semantic Equality for {LeftFilePath}", left.FilePath);
        var leftTree = await left.GetSyntaxTreeAsync().ConfigureAwait(false);
        var rightTree = await right.GetSyntaxTreeAsync().ConfigureAwait(false);
        var leftRoot = await leftTree!.GetRootAsync().ConfigureAwait(false);
        var rightRoot = await rightTree!.GetRootAsync().ConfigureAwait(false);

        // return leftRoot.ToString() == rightRoot.ToString();
        Logger.LogDebug("Getting left semantic model");
        var leftCompilation = await left.Project.GetCompilationAsync().ConfigureAwait(false);
        var leftSemanticModel = leftCompilation!.GetSemanticModel(leftTree!);
        Logger.LogDebug("Getting right semantic model");
        var rightCompilation = await right.Project.GetCompilationAsync().ConfigureAwait(false);
        var rightSemanticModel = rightCompilation!.GetSemanticModel(rightTree!);
        Logger.LogDebug("Evaluating Equality");


        var res = await GetSemanticallyUnequal(leftRoot, rightRoot, leftSemanticModel, rightSemanticModel, left, right).ConfigureAwait(false); // We can just push down the semantic model getting now that we are passing down the document.
        if (res.IsT0 && res.AsT0.MethodIdentifiers.Any())
        {
            Logger.LogDebug("Resulting diffs = " + string.Join(",", res.AsT0.MethodIdentifiers));
        }
        
        return res;
    }

    // The idea here is that we can apply semantic equality bit by bit always falling back to a string comparison.
    private static async Task<OneOf<DifferencesLimitedToFunctions, CouldntLimitToFunctions>> GetSemanticallyUnequal(SyntaxNode left, SyntaxNode right, SemanticModel leftSemanticModel, SemanticModel rightSemanticModel, Document leftDocument, Document rightDocument)
    {
        return (left, right) switch
        {
            (CompilationUnitSyntax l, CompilationUnitSyntax r) => await GetSemanticallyUnequalCompilationUnit(l, r, leftSemanticModel, rightSemanticModel, leftDocument, rightDocument).ConfigureAwait(false),
            (BaseNamespaceDeclarationSyntax l, BaseNamespaceDeclarationSyntax r) => await GetSemanticallyUnequalNamespace(l, r, leftSemanticModel, rightSemanticModel, leftDocument, rightDocument).ConfigureAwait(false),
            (ClassDeclarationSyntax l, ClassDeclarationSyntax r) => await GetSemanticallyUnequalClassDeclaration(l, r, leftSemanticModel, rightSemanticModel, leftDocument, rightDocument).ConfigureAwait(false),
            (MethodDeclarationSyntax l, MethodDeclarationSyntax r) => await GetSemanticallyUnequalMethodDeclaration(l, r, leftSemanticModel, rightSemanticModel, leftDocument, rightDocument).ConfigureAwait(false),
            _ => StringEqual(left, right) 
                ? OneOf<DifferencesLimitedToFunctions, CouldntLimitToFunctions>.FromT0(new DifferencesLimitedToFunctions())
                : OneOf<DifferencesLimitedToFunctions, CouldntLimitToFunctions>.FromT1(new CouldntLimitToFunctions())
        };
    }
    
    private static bool SemanticallyEqualSyntaxList<T>(SyntaxList<T> left, SyntaxList<T> right) where T : SyntaxNode
    {
        if (left.Count != right.Count)
            return false;
        foreach (var (l, r) in left.Zip(right))
        {
            if (!StringEqual(l, r))
                return false;
        }

        return true;
    }

    private static bool SemanticallyEqualSyntaxTokenList(SyntaxTokenList left, SyntaxTokenList right)
    {
        if (left.Count != right.Count)
            return false;
        foreach (var (l,r) in left.Zip(right))
        {
            if (l.ToString() != r.ToString())
                return false;
        }
        return true;
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

        return left.NormalizeWhitespace().ToString() == right.NormalizeWhitespace().ToString();
    }

    private static async Task<OneOf<DifferencesLimitedToFunctions, CouldntLimitToFunctions>> GetSemanticallyUnequalCompilationUnit(CompilationUnitSyntax left, CompilationUnitSyntax right, SemanticModel leftSemanticModel, SemanticModel rightSemanticModel, Document leftDocument, Document rightDocument)
    {
        if(!SemanticallyEqualSyntaxList(left.Externs, right.Externs)
           || !SemanticallyEqualSyntaxList(left.AttributeLists, right.AttributeLists))
            return new CouldntLimitToFunctions();
        
        var memberResult = await SemanticallyEqualMembers(left.Members, right.Members, leftSemanticModel, rightSemanticModel, leftDocument, rightDocument).ConfigureAwait(false);
        if (memberResult.IsT1 || SemanticallyEqualUsings(left.Usings, right.Usings))
            return memberResult;

        return new DifferencesLimitedToFunctions(memberResult.AsT0.MethodIdentifiers
            .Append(DiffToMethods.TOP_LEVEL_USING_FAKE_METHOD_IDENTIFIER).ToList());

    }

    private static async Task<OneOf<DifferencesLimitedToFunctions, CouldntLimitToFunctions>> GetSemanticallyUnequalNamespace(BaseNamespaceDeclarationSyntax left, BaseNamespaceDeclarationSyntax right, SemanticModel leftSemanticModel, SemanticModel rightSemanticModel, Document leftDocument, Document rightDocument)
    {
        if (SemanticallyEqualSyntaxList(left.Externs, right.Externs) &&
            SemanticallyEqualUsings(left.Usings, right.Usings) &&
            SemanticallyEqualSyntaxTokenList(left.Modifiers, right.Modifiers) &&
            SemanticallyEqualSyntaxList(left.AttributeLists, right.AttributeLists) &&
            left.Name.ToString() == right.Name.ToString())
            return await SemanticallyEqualMembers(left.Members, right.Members, leftSemanticModel, rightSemanticModel, leftDocument, rightDocument).ConfigureAwait(false);

        return OneOf<DifferencesLimitedToFunctions, CouldntLimitToFunctions>.FromT1(new CouldntLimitToFunctions());
    }
    
    private static async Task<OneOf<DifferencesLimitedToFunctions, CouldntLimitToFunctions>> GetSemanticallyUnequalClassDeclaration(ClassDeclarationSyntax left, ClassDeclarationSyntax right, SemanticModel leftSemanticModel, SemanticModel rightSemanticModel, Document leftDocument, Document rightDocument)
    {
        if (SemanticallyEqualSyntaxList(left.AttributeLists, right.AttributeLists) &&
            SemanticallyEqualBaseList(left.BaseList, right.BaseList) &&
            SemanticallyEqualSyntaxList(left.ConstraintClauses, right.ConstraintClauses) &&
            left.Identifier.IsEquivalentTo(right.Identifier) &&
            NullableStringEqual(left.TypeParameterList, right.TypeParameterList) &&
            SemanticallyEqualSyntaxTokenList(left.Modifiers, right.Modifiers))
            return await SemanticallyEqualMembers(left.Members, right.Members, leftSemanticModel, rightSemanticModel, leftDocument, rightDocument).ConfigureAwait(false);

        return OneOf<DifferencesLimitedToFunctions, CouldntLimitToFunctions>.FromT1(new CouldntLimitToFunctions());
    }

    private static async Task<OneOf<DifferencesLimitedToFunctions, CouldntLimitToFunctions>> GetSemanticallyUnequalMethodDeclaration(MethodDeclarationSyntax left, MethodDeclarationSyntax right, SemanticModel leftSemanticModel, SemanticModel rightSemanticModel,Document leftDocument, Document rightDocument)
    {
        var leftId = SemanticSimplifier.GetMethodIdentifier(left);
        var rightId = SemanticSimplifier.GetMethodIdentifier(right);

        if (leftId != rightId)
            return new CouldntLimitToFunctions();

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
            return new DifferencesLimitedToFunctions(new List<MethodIdentifier> {SemanticSimplifier.GetMethodIdentifier(left)});

        switch (left.Body, right.Body)
        {
            case (null,null):
                return new DifferencesLimitedToFunctions();
            case (null, {}):
                return new DifferencesLimitedToFunctions(new List<MethodIdentifier> {SemanticSimplifier.GetMethodIdentifier(left)});
            case ({},null):
                return new DifferencesLimitedToFunctions(new List<MethodIdentifier> {SemanticSimplifier.GetMethodIdentifier(left)});
        }
        
        // Calculating Renames is more expensive so lets only do it for methods that are not equal before renaming:
        if (LocalStatementsEquality.SemanticallyEqualLocalStatements(left.Body.Statements, right.Body.Statements,
                leftSemanticModel, rightSemanticModel, new List<(ISymbol left, string right)>()))
        {
            return new DifferencesLimitedToFunctions();
        }

        var sw = Stopwatch.StartNew();
        var proposedRenames = await LocalVariableRenamer.GetProposedLocalVariableRenames(left, right, leftSemanticModel, rightSemanticModel,
            leftDocument, rightDocument).ConfigureAwait(false);
        Logger.LogDebug(SemtexLog.GetPerformanceStr(nameof(LocalVariableRenamer.GetProposedLocalVariableRenames), sw.ElapsedMilliseconds));

        if (LocalStatementsEquality.SemanticallyEqualLocalStatements(left.Body.Statements, right.Body.Statements, leftSemanticModel, rightSemanticModel, proposedRenames))
        {
            return new DifferencesLimitedToFunctions();
        }

        return new DifferencesLimitedToFunctions(new List<MethodIdentifier> {SemanticSimplifier.GetMethodIdentifier(left)});
    }
    
    private static bool SemanticallyEqualUsings(SyntaxList<UsingDirectiveSyntax> left, SyntaxList<UsingDirectiveSyntax> right)
    {        
        // Allow reordering of using directives.
        var leftUsings = left.Select(u => u.NormalizeWhitespace().ToString()).ToHashSet();
        return leftUsings.SetEquals(right.Select(u => u.NormalizeWhitespace().ToString()));
    }
    private static async Task<OneOf<DifferencesLimitedToFunctions, CouldntLimitToFunctions>> SemanticallyEqualMembers(SyntaxList<MemberDeclarationSyntax> left, SyntaxList<MemberDeclarationSyntax> right, SemanticModel leftSemanticModel, SemanticModel rightSemanticModel, Document leftDocument, Document rightDocument)
    {
        // probably worth just making this into its own method - pairwise equality;
        if (left.Count != right.Count)
            return OneOf<DifferencesLimitedToFunctions, CouldntLimitToFunctions>.FromT1(new CouldntLimitToFunctions());

        var accumulator = new List<MethodIdentifier>();
        foreach (var (lMem,rMem) in left.Zip(right))
        {

            var memberResult = await GetSemanticallyUnequal(lMem, rMem, leftSemanticModel, rightSemanticModel, leftDocument, rightDocument)
                    .ConfigureAwait(false);

            if (memberResult.IsT1) return memberResult;
            accumulator.AddRange(memberResult.AsT0.MethodIdentifiers);
        }

        return OneOf<DifferencesLimitedToFunctions, CouldntLimitToFunctions>.FromT0(new DifferencesLimitedToFunctions(accumulator));
    }

    private static bool StringEqual(SyntaxNode left, SyntaxNode right)
    {
        return left.NormalizeWhitespace().ToString() == right.NormalizeWhitespace().ToString();
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

        return left.NormalizeWhitespace().ToString() == right.NormalizeWhitespace().ToString();
    }
}

internal class B
{
}
