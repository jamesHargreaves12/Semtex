using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Semtex.Models;

namespace Semtex.Semantics;

public class DiffToMethods
{
    /// <summary>
    /// If all the changes in a function are within functions then we will only apply the analyzers that are located within those functions.
    /// </summary>
    /// <param name="filepaths"></param>
    /// <param name="lineChangeMapping"></param>
    /// <returns></returns>
    public static async Task<Dictionary<AbsolutePath, HashSet<string>>> GetChangesFilter(
        HashSet<AbsolutePath> filepaths, Dictionary<AbsolutePath, List<LineDiff>> lineChangeMapping)
    {
        var result = new Dictionary<AbsolutePath, HashSet<string>>();
        foreach (var filepath in filepaths)
        {
            if (!lineChangeMapping.TryGetValue(filepath, out var lineDiffs)) continue;

            var fileText = await File.ReadAllTextAsync(filepath.Path).ConfigureAwait(false);

            var changedMethods = new Dictionary<string,List<LineDiff>>();

            var fileLines = SourceText.From(fileText).Lines;

            var root = await CSharpSyntaxTree.ParseText(fileText).GetRootAsync();
            var changeOutsideMethod = false;
            foreach (var lineDiff in lineDiffs)
            {
                if (!TryGetMethodIdentifier(lineDiff, fileLines, root, out var methodIdentifier))
                {
                    changeOutsideMethod = true;
                    continue; // TODO this should probably be a break but I don't think it makes much difference
                }

                if (!changedMethods.ContainsKey(methodIdentifier!))
                    changedMethods[methodIdentifier!] = new List<LineDiff>();
                changedMethods[methodIdentifier!].Add(lineDiff);

            }
            
            if (!changeOutsideMethod)
            {
                result[filepath] = changedMethods.Keys.ToHashSet();
            }
        }

        return result;
    }

    private static bool TryGetMethodIdentifier(LineDiff lineDiff, TextLineCollection fileLines, SyntaxNode root, out string? methodIdentifier)
    {
        var startI = Math.Max(0, lineDiff.Start - 1);
        var start = fileLines[startI].Start;
        var endI = Math.Max(lineDiff.Start + lineDiff.Count - 2,
            startI); //count is inclusive of first line, 0 indicates insert
        var end = fileLines[endI].EndIncludingLineBreak;
        var span = new TextSpan(start, end - start);
        var node = root!.FindNode(span);
        while (true)
        {
            if (node is MethodDeclarationSyntax methodDeclarationSyntax) // need other cases here e.g. getters
            {
                methodIdentifier = SemanticSimplifier.GetMethodIdentifier(methodDeclarationSyntax);
                return true;
            }

            if (node is ClassDeclarationSyntax or CompilationUnitSyntax or NamespaceDeclarationSyntax)
            {
                methodIdentifier = null;
                return false;
            }

            node = node.Parent;
        }
    }

    internal static (string semanticDiff,string unsemanticDiff) SplitDiffByChanged(string sourceText, string targetText,
        HashSet<string> changedMethodIdentifiers, List<(LineDiff,LineDiff,string)> diffs)
    {
        var srcFileLines = SourceText.From(sourceText).Lines;
        var srcRoot = CSharpSyntaxTree.ParseText(sourceText).GetRoot();
        var targetFileLines = SourceText.From(targetText).Lines;
        var targetRoot = CSharpSyntaxTree.ParseText(targetText).GetRoot();
        var semanticDiff = new StringBuilder();
        var unsemanticDiff = new StringBuilder();
        foreach (var (srcDiff, targetDiff, text) in diffs)
        {
            if (!TryGetMethodIdentifier(srcDiff, srcFileLines, srcRoot, out var srcMethodIdentifier)
                || !TryGetMethodIdentifier(targetDiff, targetFileLines, targetRoot, out var targetMethodIdentifier)
                || srcMethodIdentifier != targetMethodIdentifier // TODO think about this line some more
               )
            {
                return (string.Join("\n", diffs.Select(x => x.Item3)), "");
            }

            if (changedMethodIdentifiers.Contains(srcMethodIdentifier!))
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