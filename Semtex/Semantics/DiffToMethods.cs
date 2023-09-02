using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Semtex.Logging;
using Semtex.Models;

namespace Semtex.Semantics;

public sealed class DiffToMethods
{
    public static readonly MethodIdentifier TOP_LEVEL_USING_FAKE_METHOD_IDENTIFIER = new MethodIdentifier("<top-level-usings>", "<top-level-usings>");
    private static readonly ILogger<DiffToMethods> Logger = SemtexLog.LoggerFactory.CreateLogger<DiffToMethods>();
    /// <summary>
    /// If all the changes in a function are within functions then we will only apply the analyzers that are located within those functions.
    /// </summary>
    /// <param name="filepaths"></param>
    /// <param name="lineChangeMapping"></param>
    /// <returns></returns>
    internal static async Task<Dictionary<AbsolutePath, HashSet<MethodIdentifier>>> GetChangesFilter(
        HashSet<AbsolutePath> filepaths, Dictionary<AbsolutePath, List<LineDiff>> lineChangeMapping)
    {
        var result = new Dictionary<AbsolutePath, HashSet<MethodIdentifier>>();
        foreach (var filepath in filepaths)
        {
            if (!lineChangeMapping.TryGetValue(filepath, out var lineDiffs)) continue;

            var fileText = await File.ReadAllTextAsync(filepath.Path).ConfigureAwait(false);

            var changedMethods = new HashSet<MethodIdentifier>();

            var fileLines = SourceText.From(fileText).Lines;

            var root = await CSharpSyntaxTree.ParseText(fileText).GetRootAsync().ConfigureAwait(false);
            var changeOutsideMethod = false;
            foreach (var lineDiff in lineDiffs)
            {
                if (!TryGetMethodIdentifier(lineDiff, fileLines, root, out var methodIdentifier) || methodIdentifier is null)
                {
                    changeOutsideMethod = true;
                    break;
                }

                changedMethods.Add(methodIdentifier!.Value);
            }
            
            if (!changeOutsideMethod)
            {
                result[filepath] = changedMethods;
            }
        }

        return result;
    }

    private static bool TryGetMethodIdentifier(LineDiff lineDiff, TextLineCollection fileLines, SyntaxNode root, out MethodIdentifier? methodIdentifier)
    {
        var startI = Math.Max(0, lineDiff.Start - 1);
        var start = fileLines[startI].Start;
        var endI = Math.Max(lineDiff.Start + lineDiff.Count - 2,
            startI); //count is inclusive of first line, 0 indicates insert
        var end = fileLines[endI].EndIncludingLineBreak;
        var span = new TextSpan(start, end - start);
        var node = root.FindNode(span);
        while (true)
        {
            switch (node)
            {
                // need other cases here e.g. getters
                case MethodDeclarationSyntax methodDeclarationSyntax:
                    methodIdentifier = SemanticSimplifier.GetMethodIdentifier(methodDeclarationSyntax);
                    return true;
                case UsingDirectiveSyntax { Parent: CompilationUnitSyntax }:
                    methodIdentifier = TOP_LEVEL_USING_FAKE_METHOD_IDENTIFIER; // Special casing top level using as they will often change, methodIdentifier should be a union.
                    return true;
                case ClassDeclarationSyntax or CompilationUnitSyntax or NamespaceDeclarationSyntax:
                    methodIdentifier = null;
                    return false;
                case null:
                    methodIdentifier = null;
                    return false;
            }
            node = node.Parent;
        }
    }

    internal static (string semanticDiff, string unsemanticDiff) SplitDiffByChanged(string sourceText,
        string targetText,
        HashSet<MethodIdentifier> changedMethodIdentifiers, List<(LineDiff, LineDiff)> diffs,
        List<(LineDiff left, LineDiff right, string text)> diffWithContext)
    {
        var srcFileLines = SourceText.From(sourceText).Lines;
        var srcRoot = CSharpSyntaxTree.ParseText(sourceText).GetRoot();
        var targetFileLines = SourceText.From(targetText).Lines;
        var targetRoot = CSharpSyntaxTree.ParseText(targetText).GetRoot();

        var semanticIndices = new HashSet<int>();
        foreach (var (srcDiff, targetDiff) in diffs)
        {
            var lineChangesWithContext = diffWithContext
                .Select((val, i) => (val, i))
                .Where(item => item.val.left.Contains(srcDiff))
                .ToList();

            switch (lineChangesWithContext.Count)
            {
                case 0:
                    Logger.LogDebug("--unified=0 diff {Src} not contained in normal diff hunk", srcDiff);
                    return (string.Join("\n", diffWithContext.Select(x => x.text)), "");
                case > 1:
                    Logger.LogDebug("--unified=0 diff {Src} contained in {Count} normal diff hunks", srcDiff,
                        lineChangesWithContext.Count);
                    return (string.Join("\n", diffWithContext.Select(x => x.text)), "");
            }

            var (withContext, i) = lineChangesWithContext.Single();

            if (!withContext.right.Contains(targetDiff))
            {
                Logger.LogDebug(
                    "targetDiff not contained in same hunk as srcDiff: \n{SrcDiff} in {Left}\n{TargetDiff} not in {Right}",
                    srcDiff, withContext.left, targetDiff, withContext.right);
                return (string.Join("\n", diffWithContext.Select(x => x.text)), "");
            }

            var srcDiffInMethod = TryGetMethodIdentifier(srcDiff, srcFileLines, srcRoot, out var srcMethodIdentifier);
            var tgtDiffInMethod = TryGetMethodIdentifier(targetDiff, targetFileLines, targetRoot, out var targetMethodIdentifier); 
            
            if (!srcDiffInMethod && !tgtDiffInMethod)
            {
                continue; // If they are both at class level and we have limited semantic changes to a set of methods then this change must not be semantic
            }

            if (srcDiffInMethod && !tgtDiffInMethod && !changedMethodIdentifiers.Contains(srcMethodIdentifier!.Value))
            {
                continue; // If target diff outside method and source diff in a method that is shown to be safe then change is safe
            }

            if (srcMethodIdentifier == targetMethodIdentifier && !changedMethodIdentifiers.Contains(srcMethodIdentifier!.Value))
            {
                continue; // If the methods are in the same method and that method is shown safe then change is safe.
            }

            // either src and target in different methods or they are in a method that contains semantic change. 
            semanticIndices.Add(i);

        }

        var semanticDiff = new StringBuilder();
        var unsemanticDiff = new StringBuilder();
        foreach (var ((src, trg, text), i) in diffWithContext.Select((val, i) => (val, i)))
        {
            if (semanticIndices.Contains(i))
            {
                semanticDiff.AppendLine(text);
            }
            else
            {
                unsemanticDiff.AppendLine(text);
            }
        }

        return (semanticDiff.ToString(), unsemanticDiff.ToString());
    }

}