using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CliWrap.Exceptions;
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

    public static async Task<bool> Run(string repo, string target, string source, AbsolutePath? analyzerConfigPath, string? relativeProjFilter, AbsolutePath? explicitProjectFileMap)
    {
        var gitRepo = await GetGitRepoAndCheckClean(repo).ConfigureAwait(false);
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
    
    public static async Task<bool> RunAllAncestors(string repo, string target, AbsolutePath? analyzerConfigPath,
        string? relativeProjFilter, AbsolutePath? projectMappingFilepath, AbsolutePath outputPath)
    {
        var gitRepo = await GetGitRepoAndCheckClean(repo).ConfigureAwait(false);
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

    private static async Task<GitRepo> GetGitRepo(string pathOrUrl)
    {
        if (Utils.IsRepoUrl(pathOrUrl))
        {
            return await SetupGitRepo(pathOrUrl).ConfigureAwait(false);
        }
        
        var localChangesRepo = await GitRepo.SetupFromExistingFolder(new AbsolutePath(pathOrUrl)).ConfigureAwait(false);
        var ghostRepo = await SetupGitRepo(localChangesRepo.RemoteUrl).ConfigureAwait(false);
        return ghostRepo;
    }

    private static async Task<GitRepo> GetGitRepoAndCheckClean(string repoUrl)
    {
        var gitRepo = await GetGitRepo(repoUrl).ConfigureAwait(false);
        var diff = await gitRepo.Diff(true).ConfigureAwait(false) 
                   + await gitRepo.Diff(false).ConfigureAwait(false);
        if (diff.Any())
        {
            throw new Exception($"Expected {gitRepo.RootFolder} to be clean but it is not");
        }

        return gitRepo;
    }

    public static async Task<bool> RunModified(AbsolutePath path, AbsolutePath? analyzerConfigPath, bool staged, AbsolutePath? projFilter,
        AbsolutePath? explicitProjectFileMap)
    {
        // Get a patch from the local version of the repo and grab the commit.
        // Setup a repo in the scratch space at the same base commit.
        // apply the patch.
        // Check the patch commit for semantic equality.
        var localChangesRepo = await GitRepo.SetupFromExistingFolder(path).ConfigureAwait(false);
        var currentBaseCommit = await localChangesRepo.GetCurrentCommitSha().ConfigureAwait(false);
        var patchText = await localChangesRepo.Diff(staged).ConfigureAwait(false);
        if (patchText.Length == 0)
        {
            Logger.LogInformation("No changes found, exiting");
            return true;
        }

        var patchFilepath = Path.Join(ScratchSpacePath, "tmp.patch"); // add a guid here
        await File.WriteAllTextAsync(patchFilepath, patchText).ConfigureAwait(false);

        var ghostRepo = await SetupGitRepo(localChangesRepo.RemoteUrl).ConfigureAwait(false);
        await ghostRepo.Checkout(currentBaseCommit).ConfigureAwait(false);
        await ghostRepo.ApplyPatch(patchFilepath).ConfigureAwait(false);
        var commitSha = await ghostRepo.AddAllAndCommit().ConfigureAwait(false);
        var result = await CheckSemanticEquivalence.CheckSemanticallyEquivalent(ghostRepo, commitSha, analyzerConfigPath, projFilter, explicitProjectFileMap)
            .ConfigureAwait(false);
        var prettySummary = await DisplayResults.GetPrettySummaryOfResultsAsync(result, ghostRepo, "A commit with local changes").ConfigureAwait(false);
        Logger.LogInformation("\n\n{PrettySummary}",prettySummary);

        return result.SemanticallyEquivalent;
    }

    private static async Task<GitRepo> SetupGitRepo(string repo)
    {
        // Clean / Create the temp directory for the build.
        var repoName = repo.Split("/")[^1].Split(".")[0];
        var rootFolder = new AbsolutePath(Path.Join(ScratchSpacePath, repoName));

        if(Directory.Exists(rootFolder.Path))
        {
            Logger.LogInformation("Folder {RootFolder} already exists. Checking if it has the correct origin", rootFolder);
            try
            {

                var existingRepo = await GitRepo.SetupFromExistingFolder(rootFolder).ConfigureAwait(false);
                // TODO Probably should do some clean here etc for sanity sake.
                if (existingRepo.RemoteUrl.Replace(".git","") == repo.Replace(".git",""))
                {
                    await existingRepo.Fetch().ConfigureAwait(false);
                    return existingRepo;
                }
            }
            catch (CommandExecutionException e)
            {
                Logger.LogWarning($"Failed to load repo from existing folder with message {e}");
                // Will just continue and check it out from scratch.
            }
        }
    
        // Should be clever here and just check if it is the same repo and just git clean if so.
        Utils.EnsureDirectoryExistsAndEmpty(rootFolder);

        // Clone the repo into the temp directory 
        var gitRepo = await GitRepo.Clone(repo, rootFolder).ConfigureAwait(false);
        return gitRepo;
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
}