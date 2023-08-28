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
        foreach (var (c,i) in commits.Select((x,i)=> (x,i)))
        {
            Logger.LogInformation("Progress = {Pct}%, Now checking {C} ", i*100/commits.Count , c);
            var result = await CheckSemanticEquivalence.CheckSemanticallyEquivalent(gitRepo, c, analyzerConfigPath, projFilter, projectMappingFilepath)
                .ConfigureAwait(false);
            var prettySummary = await DisplayResults.GetPrettySummaryOfResultsAsync(result, gitRepo).ConfigureAwait(false);
            Logger.LogInformation("Results for {C}",c);
            Logger.LogInformation("\n\n{PrettySummary}",prettySummary);

            var jsonSummaries = results.Select(res => JsonSerializer.Serialize(res));
            await File.WriteAllLinesAsync($"{outputPath.Path}/Results.jsonl", jsonSummaries).ConfigureAwait(false);
            var sw = Stopwatch.StartNew();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Logger.LogInformation(SemtexLog.GetPerformanceStr("GC", sw.ElapsedMilliseconds));
            results.Add(result);
        }


        var prettyBuilder = new StringBuilder();
        foreach (var res in results)
        {
            prettyBuilder.Append(await DisplayResults.GetPrettySummaryOfResultsAsync(res, gitRepo).ConfigureAwait(false));
        }

        Logger.LogInformation(prettyBuilder.ToString());

        
        return results.All(r => r.SemanticallyEquivalent);
    }


    public static async Task<bool> RunModified(AbsolutePath path, AbsolutePath? analyzerConfigPath, bool staged,
        AbsolutePath? projFilter,
        AbsolutePath? explicitProjectFileMap)
    {
        var gitRepo = await CreateGitRepoWithLocalChangesCommitted(path, staged ? IncludeUncommittedChanges.Staged : IncludeUncommittedChanges.Unstaged);
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

    public static async Task Split(string repoPath, string baseCommit, IncludeUncommittedChanges includeUncommitted, string? projectMap)
    {
        var gitRepo = await CreateGitRepoWithLocalChangesCommitted(new AbsolutePath(repoPath), includeUncommitted);
        
        var projectMapPath = projectMap is null ? null : new AbsolutePath(projectMap);
        
        // Generate a patch so that we are only comparing a single commit.
        var patchText = await gitRepo.Diff(baseCommit, "HEAD");
        
        await SplitPatch(gitRepo, baseCommit, patchText, projectMapPath);
    }
    
    public static async Task SplitRemote(string repoUrl, string target, string baseCommit, string? projectMap)
    {
        var gitRepo = await GitRepo.CreateGitRepoFromUrl(repoUrl);


        baseCommit = await gitRepo.GetMergeBase(target, baseCommit);
        
        // Generate a patch so that we are only comparing a single commit.
        var patchText = await gitRepo.Diff(baseCommit,target);
        var projectMapPath = projectMap is null ? null : new AbsolutePath(projectMap);

        await SplitPatch(gitRepo, baseCommit, patchText, projectMapPath);
    }

    private static async Task SplitPatch(GitRepo gitRepo, string baseCommit, string patchText, AbsolutePath projectMapPath)
    {
        
        var patchFilepath = Path.Join(ScratchSpacePath, $"test-{Guid.NewGuid()}.patch");
        await File.WriteAllTextAsync(patchFilepath, patchText).ConfigureAwait(false);

        await gitRepo.Checkout(baseCommit);

        await gitRepo.ApplyPatch(new AbsolutePath(patchFilepath)).ConfigureAwait(false);
        await gitRepo.AddAllAndCommit().ConfigureAwait(false);

        var commitWithPatch = await gitRepo.GetCurrentCommitSha().ConfigureAwait(false);

        await FindSplit(gitRepo, commitWithPatch, projectMapPath).ConfigureAwait(false);
    }

    private static async Task FindSplit(GitRepo gitRepo, string commit, AbsolutePath? projectMapPath)
    {
        var result = await CheckSemanticEquivalence
            .CheckSemanticallyEquivalent(gitRepo, commit, null, null, projectMappingFilepath: projectMapPath)
            .ConfigureAwait(false);
        // Create a patch of the difference between target and source and then apply that.
        var semanticallyEquivalentStatuses = new[] {Status.SemanticallyEquivalent, Status.OnlyRename, Status.SafeFile }; // Need to Consider SomeMethodsEquivalent
        var unsemanticChangesBuilder = new StringBuilder();
        var semanticChangesBuilder = new StringBuilder();
        
        foreach (var file in result.FileModels)
        {
            var srcSha = $"{commit}~1";
            var fullDiff = await gitRepo.GetFileDiff(srcSha, commit, file.Filepath).ConfigureAwait(false);
            // Probably just make this a switch statemenmt
            if(semanticallyEquivalentStatuses.Contains(file.Status))
            {
                unsemanticChangesBuilder.Append(fullDiff);
            }
            else if (file.Status == Status.SubsetOfDiffEquivalent)
            {
                var srcFilepath = gitRepo.RootFolder.Join(file.Filepath);
                var targetFilepath = result.DiffConfig.GetTargetFilepath(srcFilepath);
                var lineChanges = await gitRepo.GetLineChanges(srcSha, commit, srcFilepath).ConfigureAwait(false);
                var lineChangesWithContex = await gitRepo.GetLineChangesWithContex(srcSha, commit, srcFilepath).ConfigureAwait(false);
                var sourceText = await gitRepo.GetFileTextAtCommit(srcSha, srcFilepath).ConfigureAwait(false);
                var targetText = await gitRepo.GetFileTextAtCommit(commit, targetFilepath).ConfigureAwait(false);
                var (semanticDiff,unsemanticDiff) = DiffToMethods.SplitDiffByChanged(sourceText, targetText, file.SubsetOfMethodsThatAreNotEquivalent!, lineChanges, lineChangesWithContex);
                var header = string.Join("\n", fullDiff.Split("\n").Take(4));
                
                if (semanticDiff.Any())
                {
                    semanticChangesBuilder.AppendLine(header);
                    semanticChangesBuilder.Append(semanticDiff);
                }
                
                if (unsemanticDiff.Any())
                {
                    unsemanticChangesBuilder.AppendLine(header);
                    unsemanticChangesBuilder.Append(unsemanticDiff);
                }
            }
            else
            {
                semanticChangesBuilder.Append(fullDiff);
            }
        }
        
        var resultStr = await DisplayResults.GetPrettySummaryOfResultsAsync(result, gitRepo, "changes").ConfigureAwait(false);
        Logger.LogInformation(resultStr);

        var semanticFilepath = Path.Join(ScratchSpacePath, "change_behaviour.patch");
        var unsemanticFilepath = Path.Join(ScratchSpacePath, "improve_readability.patch");
        if (Path.Exists(semanticFilepath))
            File.Delete(semanticFilepath);
        
        if(Path.Exists(unsemanticFilepath))
            File.Delete(unsemanticFilepath);

        var applyBuilder = new StringBuilder();

        if (semanticChangesBuilder.Length > 0)
        {
            await File.WriteAllTextAsync(semanticFilepath, semanticChangesBuilder.ToString());
            Logger.LogInformation("Changes that DO effect runtime behaviour at: {semanticChanges}", semanticFilepath);
            applyBuilder.AppendLine($"git apply {semanticFilepath}");
        }

        if (unsemanticChangesBuilder.Length > 0)
        {       
            await File.WriteAllTextAsync(unsemanticFilepath, unsemanticChangesBuilder.ToString());
            Logger.LogInformation("Changes that do NOT effect runtime behaviour at: {UnsemanticChanges}", unsemanticFilepath);
            applyBuilder.AppendLine($"git apply {unsemanticFilepath}");
        }

        
        Logger.LogInformation("To apply:\n\n{ApplyBuilder}", applyBuilder.ToString());
    }


    private static async Task<GitRepo> CreateGitRepoWithLocalChangesCommitted(AbsolutePath path, IncludeUncommittedChanges includeUncommittedChanges)
    {
        // Get a patch from the local version of the repo and grab the commit.
        // Setup a repo in the scratch space at the same base commit.
        // apply the patch.
        // Check the patch commit for semantic equality.
        var localChangesRepo = await GitRepo.SetupFromExistingFolder(path).ConfigureAwait(false);
        var ghostRepo = await GitRepo.CreateGitRepoFromUrl(localChangesRepo.RemoteUrl).ConfigureAwait(false);
        var currentBaseCommit = await localChangesRepo.GetCurrentCommitSha().ConfigureAwait(false);
        await ghostRepo.Checkout(currentBaseCommit).ConfigureAwait(false);

        if (includeUncommittedChanges == IncludeUncommittedChanges.None) return ghostRepo;
        
        var patchFilepath = new AbsolutePath(Path.Join(ScratchSpacePath, $"tmp_{new Guid()}.patch"));

        var hasLocalChanges = await localChangesRepo.CreatePatchFileOfLocalChanges(patchFilepath, includeUncommittedChanges).ConfigureAwait(false);

        if (!hasLocalChanges) return ghostRepo;
        
        await ghostRepo.ApplyPatch(patchFilepath).ConfigureAwait(false);
        await ghostRepo.AddAllAndCommit().ConfigureAwait(false);
        
        return ghostRepo;
    }
    

}