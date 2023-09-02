using System.Diagnostics;
using Semtex.Rewriters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.Extensions.Logging;
using Semtex.Logging;
using Semtex.Models;

namespace Semtex.Semantics;

internal class SemanticSimplifier
{
    private static readonly ILogger<SemanticSimplifier> Logger =
        SemtexLog.LoggerFactory.CreateLogger<SemanticSimplifier>();

    internal static async Task<Solution> GetSolutionWithFilesSimplified(
        Solution sln, HashSet<ProjectId> projectIds, HashSet<AbsolutePath> documentsToSimplify, AbsolutePath? analyzerConfigPath,
        Dictionary<AbsolutePath, HashSet<MethodIdentifier>> changedMethodsMap)
    {
        var progressBar = new ProgressBar<SemanticSimplifier>(projectIds.Count, Logger);
        foreach (var (i, projId) in projectIds.Select((x, i) => (i, x))) // This could 100% be parallelized for speed - for now won't do this as the logging becomes more difficult.
        {
            progressBar.Update(i);
            var proj = sln.GetProject(projId)!;
            var docsToSimplify = proj.Documents.Where(d => documentsToSimplify.Contains(new AbsolutePath(d.FilePath!))).ToList();
            var docIdsToSimplify = docsToSimplify.Select(d => d.Id).ToList();

            var idToChangedMethodsMap = docsToSimplify
                .Where(d => changedMethodsMap.ContainsKey(new AbsolutePath(d.FilePath!)))
                .ToDictionary(
                    d => d.Id,
                    d => changedMethodsMap[new AbsolutePath(d.FilePath!)]
                );

            // Simplify docs
            var progress = new Progress<double>();
            progress.ProgressChanged += (_, value) => progressBar.Update(i + 0.9 * value);
            sln = await SafeAnalyzers.Apply(sln, projId, docIdsToSimplify, analyzerConfigPath, idToChangedMethodsMap, progress).ConfigureAwait(false);
            progressBar.Update(i + 0.9);
            sln = await ApplyRewriters(sln, docIdsToSimplify).ConfigureAwait(false);
        }
        progressBar.Update(projectIds.Count);

        return sln;
    }


    private static List<CSharpSyntaxRewriter> _rewriters = new()
    {
        new RemoveTriviaRewriter(),
        // new ConsistentOrderRewriter(), want to do this after renaming so it is called from CoSimplifySolutions
        new RemoveSuppressNullableWarningRewriter(),
        new ApplySimplificationServiceRewriter(),
        new TrailingCommaRewriter()
    };
    private static async Task<Solution> ApplyRewriters(Solution sln, IEnumerable<DocumentId> docsToSimplify)
    {
        var sw = Stopwatch.StartNew();

        foreach (var docId in docsToSimplify)
        {
            var doc = sln.GetDocument(docId)!;
            var rootNode = (await doc.GetSyntaxRootAsync().ConfigureAwait(false))!;
            foreach (var rewriter in _rewriters)
            {
                rootNode = rewriter.Visit(rootNode);
            }
            var normalizedWhiteSpace = rootNode.NormalizeWhitespace();
            sln = sln.WithDocumentSyntaxRoot(doc.Id, normalizedWhiteSpace);

            var simplifiedDoc = await Simplifier.ReduceAsync(sln.GetDocument(doc.Id)!, Simplifier.Annotation).ConfigureAwait(false);
            sln = sln.WithDocumentSyntaxRoot(doc.Id, (await simplifiedDoc.GetSyntaxRootAsync().ConfigureAwait(false))!);
        }

        Logger.LogDebug(SemtexLog.GetPerformanceStr(nameof(ApplyRewriters), sw.ElapsedMilliseconds));
        return sln;
    }

    public static MethodIdentifier GetMethodIdentifier(MethodDeclarationSyntax method)
    {
        var overloadIdentifier = string.Join("_",
            method.ParameterList.Parameters.Select(x => x.Type!.ToString()).ToArray());

        return new MethodIdentifier(method.Identifier.ToString(), overloadIdentifier);
    }
}