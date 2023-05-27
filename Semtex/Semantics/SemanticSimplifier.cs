using System.Diagnostics;
using Semtex.Rewriters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Semtex.Logging;

namespace Semtex.Semantics;

internal class SemanticSimplifier
{
    private static readonly ILogger<SemanticSimplifier> Logger =
        SemtexLog.LoggerFactory.CreateLogger<SemanticSimplifier>();

    internal static async Task<(Solution sln, HashSet<string> failedToCompile)> GetSolutionWithFilesSimplified(
        Solution sln, Dictionary<string, HashSet<string>> projectToFilesMap, string? analyzerConfigPath,
        Dictionary<string, HashSet<string>> changedMethodsMap)
    {
        var failedToCompile = new HashSet<string>();
        var projectIds = sln.Projects
            .Where(p => p.FilePath != null && projectToFilesMap.ContainsKey(p.FilePath) &&
                        projectToFilesMap[p.FilePath].Count > 0)
            .Select(p => p.Id)
            .ToList();
        foreach (var projId in projectIds) // This could 100% be parallelized for speed - for now won't do this as the logging becomes more difficult.
        {
            var proj = sln.GetProject(projId)!;
            Logger.LogInformation("Processing {ProjName}", proj.Name);

            // Confirm that there are no issues by compiling it once without analyzers.
            var compileStopWatch = Stopwatch.StartNew();
            var compilation = await proj.GetCompilationAsync().ConfigureAwait(false);
            var diagnostics = compilation!.GetDiagnostics();
            var docsToSimplify = projectToFilesMap[proj.FilePath!];
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                Logger.LogError("Failed to compile before applying simplifications {Message}", diagnostics.First());
                failedToCompile.UnionWith(docsToSimplify);
                continue;
            }

            compileStopWatch.Stop();
            Logger.LogInformation(SemtexLog.GetPerformanceStr("Initial Compilation", compileStopWatch.ElapsedMilliseconds));
            // Simplify docs
            sln = await SafeAnalyzers.Apply(sln, projId, docsToSimplify, analyzerConfigPath, changedMethodsMap).ConfigureAwait(false);
            sln = await ApplyRewriters(sln, projId, docsToSimplify).ConfigureAwait(false);
        }

        return (sln, failedToCompile);
    }
    

    private static async Task<Solution> ApplyRewriters(Solution sln, ProjectId projectId,
        HashSet<string> docsToSimplify)
    {
        var simplifiedDocs = sln.GetProject(projectId)!.Documents
            .Where(d => docsToSimplify.Contains(d.FilePath!));

        foreach (var doc in simplifiedDocs)
        {
            var rootNode = ((await doc.GetSyntaxRootAsync().ConfigureAwait(false))!);
            var noTrivia = new RemoveTriviaRewriter().Visit(rootNode);
            var consistentOrder = new ConsistentOrderRewriter().Visit(noTrivia);
            var normalizedWhiteSpace = consistentOrder!.NormalizeWhitespace();
            sln = sln.WithDocumentSyntaxRoot(doc.Id, normalizedWhiteSpace);
        }

        return sln;
    }

    /// <summary>
    /// TODO improve this.
    /// </summary>
    /// <param name="method"></param>
    /// <returns></returns>
    public static string GetMethodIdentifier(MethodDeclarationSyntax method)
    {
        var overloadIdentifier = string.Join("_",
            method.ParameterList.Parameters.Select(x => x.Type!.ToString()).ToArray());
        return $"{method.Identifier}_{overloadIdentifier}";
    }

}