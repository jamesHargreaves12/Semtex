using Semtex.Rewriters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Semtex.Logging;
using Semtex.Models;

namespace Semtex.Semantics;

internal class SemanticSimplifier
{
    private static readonly ILogger<SemanticSimplifier> Logger =
        SemtexLog.LoggerFactory.CreateLogger<SemanticSimplifier>();


    internal static async Task<Solution> GetSolutionWithFilesSimplified(
        Solution sln, List<ProjectId> projectIds, Dictionary<AbsolutePath, HashSet<AbsolutePath>> projectToFilesMap, AbsolutePath? analyzerConfigPath,
        Dictionary<AbsolutePath, HashSet<string>> changedMethodsMap)
    {
        foreach (var projId in projectIds) // This could 100% be parallelized for speed - for now won't do this as the logging becomes more difficult.
        {
            var proj = sln.GetProject(projId)!;
            Logger.LogInformation("Processing {ProjName}", proj.Name);
            var docsToSimplify = projectToFilesMap[new AbsolutePath(proj.FilePath!)];

            // Simplify docs
            sln = await SafeAnalyzers.Apply(sln, projId, docsToSimplify, analyzerConfigPath, changedMethodsMap).ConfigureAwait(false);
            sln = await ApplyRewriters(sln, projId, docsToSimplify).ConfigureAwait(false);
        }

        return sln;
    }
    

    private static async Task<Solution> ApplyRewriters(Solution sln, ProjectId projectId,
        HashSet<AbsolutePath> docsToSimplify)
    {
        var stingDocsToSimplify = docsToSimplify.Select(d => d.Path).ToHashSet(); 
        var simplifiedDocs = sln.GetProject(projectId)!.Documents
            .Where(d => stingDocsToSimplify.Contains(d.FilePath!));

        foreach (var doc in simplifiedDocs)
        {
            var rootNode = (await doc.GetSyntaxRootAsync().ConfigureAwait(false))!;
            var noTrivia = new RemoveTriviaRewriter().Visit(rootNode);
            var consistentOrder = new ConsistentOrderRewriter().Visit(noTrivia);
            var withoutNullableSupression = new RemoveSuppressNullableWarningRewriter().Visit(consistentOrder);
            var normalizedWhiteSpace = withoutNullableSupression!.NormalizeWhitespace();
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