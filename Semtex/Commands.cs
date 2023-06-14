using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Semtex.Logging;
using Semtex.Models;
using Semtex.ProjectFinder;
using Semtex.Semantics;

namespace Semtex;

public sealed class Commands
{
    private static readonly string ScratchSpacePath = Path.Join(Path.GetTempPath(), "Semtex");

    private static readonly ILogger<Commands> Logger = SemtexLog.LoggerFactory.CreateLogger<Commands>();

    public static async Task<bool> Run(string repoPathOrUrl, string target, string source, AbsolutePath? analyzerConfigPath, string? relativeProjFilter, AbsolutePath? explicitProjectFileMap)
    {
        GitRepo gitRepo;
        if (Path.Exists(repoPathOrUrl))
        {
            var localChangesRepo = await GitRepo.SetupFromExistingFolder(new AbsolutePath(repoPathOrUrl)).ConfigureAwait(false);
            gitRepo = await GitRepo.CreateGitRepoFromUrl(localChangesRepo.RemoteUrl).ConfigureAwait(false);
        }
        else
        {
            gitRepo = await GitRepo.CreateGitRepoFromUrl(repoPathOrUrl).ConfigureAwait(false);
        }

        await gitRepo.AssertClean();

        await gitRepo.CheckoutAndPull(target).ConfigureAwait(false);

        var projFilter = relativeProjFilter == null ? null : gitRepo.RootFolder.Join(relativeProjFilter);

        var commits = await gitRepo.ListCommitShasBetween(source, target).ConfigureAwait(false);

        var results = new List<CommitModel>();
        foreach (var c in commits)
        {
            Logger.LogInformation("Checking {C}",c);
            var result = await CheckSemanticEquivalence.CheckSemanticallyEquivalent(gitRepo,c, analyzerConfigPath, projFilter, explicitProjectFileMap).ConfigureAwait(false);
            var resultSummary = await DisplayResults.GetPrettySummaryOfResultsAsync(result, gitRepo).ConfigureAwait(false);
            Logger.LogInformation("Results for {C}", c);
            Logger.LogInformation("\n\n{ResultSummary}", resultSummary);
            results.Add(result);
        }

        var summary = new StringBuilder();
        foreach (var result in results)
        {
            var prettyRes = await DisplayResults.GetPrettySummaryOfResultsAsync(result, gitRepo).ConfigureAwait(false);
            summary.Append(prettyRes);
        }

        Logger.LogInformation("\n\n{Summary}", summary);
        
        return results.All(r => r.SemanticallyEquivalent);
    }
    
    public static async Task<bool> RunAllAncestors(string repoPathOrUrl, string target, AbsolutePath? analyzerConfigPath,
        string? relativeProjFilter, AbsolutePath? projectMappingFilepath, AbsolutePath outputPath)
    {
        GitRepo gitRepo;
        if (Path.Exists(repoPathOrUrl))
        {
            var localChangesRepo = await GitRepo.SetupFromExistingFolder(new AbsolutePath(repoPathOrUrl)).ConfigureAwait(false);
            gitRepo = await GitRepo.CreateGitRepoFromUrl(localChangesRepo.RemoteUrl).ConfigureAwait(false);
        }
        else
        {
            gitRepo = await GitRepo.CreateGitRepoFromUrl(repoPathOrUrl).ConfigureAwait(false);
        }
        await gitRepo.AssertClean();

        await gitRepo.CheckoutAndPull(target).ConfigureAwait(false);
        var projFilter = relativeProjFilter == null ? null : gitRepo.RootFolder.Join(relativeProjFilter);

        var commits = await gitRepo.GetAllAncestors(target).ConfigureAwait(false);
        // No point doing this for the first commit in the series. Probably could be smarter here about if we hit the limit etc but I think this is good for now.
        commits.RemoveAt(commits.Count - 1);

        var results = new List<CommitModel>();
        foreach (var c in commits)
        {
            Logger.LogInformation("Checking {C}",c);
            var result = await CheckSemanticEquivalence.CheckSemanticallyEquivalent(gitRepo, c, analyzerConfigPath, projFilter, projectMappingFilepath)
                .ConfigureAwait(false);
            var prettySummary = await DisplayResults.GetPrettySummaryOfResultsAsync(result, gitRepo).ConfigureAwait(false);
            Logger.LogInformation("Results for {C}",c);
            Logger.LogInformation("\n\n{PrettySummary}",prettySummary);

            var jsonSummaries = results.Select(res => JsonSerializer.Serialize(res));
            await File.WriteAllLinesAsync($"{outputPath.Path}/Results.jsonl", jsonSummaries).ConfigureAwait(false);
            var sw = Stopwatch.StartNew();
            // TODO Test without these
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Logger.LogInformation(SemtexLog.GetPerformanceStr("GC", sw.ElapsedMilliseconds));
            results.Add(result);
        }


        var prettyBuilder = new StringBuilder();
        foreach (var res in results)
        {
            prettyBuilder.Append(await DisplayResults.GetPrettySummaryOfResultsAsync(res, gitRepo));
        }

        Logger.LogInformation(prettyBuilder.ToString());

        
        return results.All(r => r.SemanticallyEquivalent);
    }


    public static async Task<bool> RunModified(AbsolutePath path, AbsolutePath? analyzerConfigPath, bool staged,
        AbsolutePath? projFilter,
        AbsolutePath? explicitProjectFileMap)
    {
        var gitRepo = await CreateGitRepoWithLocalChangesCommitted(path);
        var commitSha = await gitRepo.GetCurrentCommitSha().ConfigureAwait(false);
        
        var result = await CheckSemanticEquivalence.CheckSemanticallyEquivalent(gitRepo, commitSha, analyzerConfigPath, projFilter, explicitProjectFileMap)
            .ConfigureAwait(false);
        var prettySummary = await DisplayResults.GetPrettySummaryOfResultsAsync(result, gitRepo, "A commit with local changes").ConfigureAwait(false);
        Logger.LogInformation("\n\n{PrettySummary}",prettySummary);

        return result.SemanticallyEquivalent;
    }


    public static async Task ComputeProjectMapping(AbsolutePath slnPath, string filepath)
    {
        var gitRepoLocation = new AbsolutePath(Path.GetDirectoryName(slnPath.Path)!);
        var localRepo = await GitRepo.SetupFromExistingFolder(gitRepoLocation).ConfigureAwait(false);
        var sln = await SolutionUtils.LoadSolution(slnPath).ConfigureAwait(false);
        var absoluteMapping = ComputeDocumentToProjMapping.ComputeDocumentToProjectMapping(sln);
        var relativeMapping = new Dictionary<string, string[]>();
        foreach (var (docPath, projPaths) in absoluteMapping)
        {
            relativeMapping[localRepo.GetRelativePath(docPath)] = projPaths.Select(path => localRepo.GetRelativePath(path)).ToArray();
        }

        await File.WriteAllTextAsync(filepath, JsonSerializer.Serialize(relativeMapping)).ConfigureAwait(false);
    }

    public static async Task Split(string repoPathOrUrl, string source, string? target ,string? projectMap)
    {
        GitRepo gitRepo;
        // This could be slow since git clone can be slow.
        if (Path.Exists(repoPathOrUrl))
        {
            gitRepo = await CreateGitRepoWithLocalChangesCommitted(new AbsolutePath(repoPathOrUrl));
        }
        else
        {
            gitRepo = await GitRepo.CreateGitRepoFromUrl(repoPathOrUrl);
        }

        target ??= await gitRepo.GetCurrentCommitSha();
        var projectMapPath = projectMap is null ? null : new AbsolutePath(projectMap);

        source = await gitRepo.GetMergeBase(source, target);
        
        // Generate a patch so that we are only comparing a single commit.
        var patchText = await gitRepo.Diff(source, target);
        
        // TODO add guid
        var patchFilepath = Path.Join(ScratchSpacePath, "test.patch");
        await File.WriteAllTextAsync(patchFilepath, patchText).ConfigureAwait(false);

        await gitRepo.Checkout(source);

        await gitRepo.ApplyPatch(new AbsolutePath(patchFilepath));
        await gitRepo.AddAllAndCommit().ConfigureAwait(false);

        var commitWithPatch = await gitRepo.GetCurrentCommitSha();

        var result = await CheckSemanticEquivalence
            .CheckSemanticallyEquivalent(gitRepo, commitWithPatch, null ,null, projectMappingFilepath: projectMapPath)
            .ConfigureAwait(false);
        
        var resultStr = await DisplayResults.GetPrettySummaryOfResultsAsync(result, gitRepo, "changes");
        
        Logger.LogInformation(resultStr);
        
        // Create a patch of the difference between target and source and then apply that.
    }
    
    private static async Task<GitRepo> CreateGitRepoWithLocalChangesCommitted(AbsolutePath path)
    {
        // Get a patch from the local version of the repo and grab the commit.
        // Setup a repo in the scratch space at the same base commit.
        // apply the patch.
        // Check the patch commit for semantic equality.
        var localChangesRepo = await GitRepo.SetupFromExistingFolder(path).ConfigureAwait(false);
        var ghostRepo = await GitRepo.CreateGitRepoFromUrl(localChangesRepo.RemoteUrl).ConfigureAwait(false);
        var currentBaseCommit = await localChangesRepo.GetCurrentCommitSha().ConfigureAwait(false);
        await ghostRepo.Checkout(currentBaseCommit).ConfigureAwait(false);

        var patchFilepath = new AbsolutePath(Path.Join(ScratchSpacePath, "tmp.patch")); //TODO add a guid here

        var hasLocalChanges = await localChangesRepo.CreatePatchFileOfLocalChanges(patchFilepath);

        if (!hasLocalChanges) return ghostRepo;
        
        await ghostRepo.ApplyPatch(patchFilepath).ConfigureAwait(false);
        await ghostRepo.AddAllAndCommit().ConfigureAwait(false);
        
        return ghostRepo;
    }
    

}