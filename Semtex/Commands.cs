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
    private static readonly AbsolutePath ScratchSpacePath = new AbsolutePath(Path.Join(Path.GetTempPath(), "Semtex"));

    private static readonly ILogger<Commands> Logger = SemtexLog.LoggerFactory.CreateLogger<Commands>();
    // If true then any errors that we unhandled errors within the solution logic will be unhandled. If false then the
    // run will complete reporting the status for the C# files as UnexpectedError - Matters most in
    // the case of --all-ancestors.
    private const bool DefaultFailFast =
#if DEBUG
    true;
#else
    false;
#endif

    internal static async Task<bool> Run(string repoPathOrUrl, string target, string source, AbsolutePath? analyzerConfigPath, string? relativeProjFilter, AbsolutePath? explicitProjectFileMap)
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

        await gitRepo.AssertClean().ConfigureAwait(false);

        await gitRepo.CheckoutAndPull(target).ConfigureAwait(false);

        var projFilter = relativeProjFilter == null ? null : gitRepo.RootFolder.Join(relativeProjFilter);

        var commits = await gitRepo.ListCommitShasBetween(source, target).ConfigureAwait(false);

        var results = new List<CommitModel>();
        foreach (var c in commits)
        {
            Logger.LogInformation("Checking {C}", c);
            var result = await CheckSemanticEquivalence.CheckSemanticallyEquivalent(gitRepo, c, analyzerConfigPath, projFilter, explicitProjectFileMap, DefaultFailFast).ConfigureAwait(false);
            var resultSummary = await DisplayResults.GetPrettySummaryOfResultsAsync(result, gitRepo).ConfigureAwait(false);
            Logger.LogDebug("Results for {C}", c);
            Logger.LogDebug("\n\n{ResultSummary}", resultSummary);
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

    internal static async Task<bool> RunAllAncestors(string repoPathOrUrl, string target, AbsolutePath? analyzerConfigPath,
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
        await gitRepo.AssertClean().ConfigureAwait(false);

        await gitRepo.CheckoutAndPull(target).ConfigureAwait(false);
        var projFilter = relativeProjFilter == null ? null : gitRepo.RootFolder.Join(relativeProjFilter);

        var commits = await gitRepo.GetAllAncestors(target).ConfigureAwait(false);
        // No point doing this for the first commit in the series. Probably could be smarter here about if we hit the limit etc but I think this is good for now.
        commits.RemoveAt(commits.Count - 1);

        var results = new List<CommitModel>();
        foreach (var (c, i) in commits.Select((x, i) => (x, i)))
        {
            Logger.LogInformation("Checking {C} ({Prc}%)", c, i * 100 / commits.Count);
            var result = await CheckSemanticEquivalence.CheckSemanticallyEquivalent(gitRepo, c, analyzerConfigPath, projFilter, projectMappingFilepath, DefaultFailFast)
                .ConfigureAwait(false);
            var prettySummary = await DisplayResults.GetPrettySummaryOfResultsAsync(result, gitRepo).ConfigureAwait(false);
            Logger.LogDebug("Results for {C}", c);
            Logger.LogDebug("\n\n{PrettySummary}", prettySummary);

            var jsonSummaries = results.Select(res => JsonSerializer.Serialize(res));
            await File.WriteAllLinesAsync($"{outputPath.Path}/Results.jsonl", jsonSummaries).ConfigureAwait(false);
            var sw = Stopwatch.StartNew();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Logger.LogDebug(SemtexLog.GetPerformanceStr("GC", sw.ElapsedMilliseconds));
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


    internal static async Task ComputeProjectMapping(AbsolutePath slnPath, string filepath)
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

    internal static async Task Split(string repoPath, string baseCommit, IncludeUncommittedChanges includeUncommitted, string? projectMap)
    {
        var (gitRepo, localRepoHead) = await CreateGitRepoWithLocalChangesCommitted(new AbsolutePath(repoPath), includeUncommitted).ConfigureAwait(false);
        // Check that git push has been run TODO
        if (baseCommit == "HEAD")
        {
            baseCommit = localRepoHead;
        }

        var projectMapPath = projectMap is null ? null : new AbsolutePath(projectMap);

        // Generate a patch so that we are only comparing a single commit.
        var patchText = await gitRepo.Diff(baseCommit, "HEAD").ConfigureAwait(false);

        await SplitPatch(gitRepo, baseCommit, patchText, projectMapPath).ConfigureAwait(false);
    }

    internal static async Task SplitRemote(string repoUrl, string target, string baseCommit, string? projectMap)
    {
        var gitRepo = await GitRepo.CreateGitRepoFromUrl(repoUrl).ConfigureAwait(false);


        baseCommit = await gitRepo.GetMergeBase(target, baseCommit).ConfigureAwait(false);

        // Generate a patch so that we are only comparing a single commit.
        var patchText = await gitRepo.Diff(baseCommit, target).ConfigureAwait(false);
        var projectMapPath = projectMap is null ? null : new AbsolutePath(projectMap);

        await SplitPatch(gitRepo, baseCommit, patchText, projectMapPath).ConfigureAwait(false);
    }

    private static async Task SplitPatch(GitRepo gitRepo, string baseCommit, string patchText, AbsolutePath? projectMapPath)
    {
        var patchFilepath = ScratchSpacePath.Join($"test-{Guid.NewGuid()}.patch");
        await File.WriteAllTextAsync(patchFilepath.Path, patchText).ConfigureAwait(false);

        await gitRepo.Checkout(baseCommit).ConfigureAwait(false);

        await gitRepo.ApplyPatch(patchFilepath).ConfigureAwait(false);
        await gitRepo.AddAllAndCommit("All local changes").ConfigureAwait(false);

        var commitWithPatch = await gitRepo.GetCurrentCommitSha().ConfigureAwait(false);

        await FindSplit(gitRepo, commitWithPatch, projectMapPath).ConfigureAwait(false);
    }

    private static async Task FindSplit(GitRepo gitRepo, string commit, AbsolutePath? projectMapPath)
    {
        var result = await CheckSemanticEquivalence
            .CheckSemanticallyEquivalent(gitRepo, commit, null, null, projectMappingFilepath: projectMapPath, DefaultFailFast)
            .ConfigureAwait(false);
        // Create a patch of the difference between target and source and then apply that.
        var unsemanticChangesBuilder = new StringBuilder();
        var semanticChangesBuilder = new StringBuilder();

        foreach (var file in result.FileModels)
        {
            var srcSha = $"{commit}~1";
            var fullDiff = await gitRepo.GetFileDiff(srcSha, commit, file.Filepath).ConfigureAwait(false);
            // Probably just make this a switch statement
            switch (file.Status)
            {
                case Status.SemanticallyEquivalent:
                case Status.OnlyRename:
                case Status.SafeFile:
                    unsemanticChangesBuilder.Append(fullDiff);
                    break;
                case Status.SubsetOfDiffEquivalent:
                    var srcFilepath = gitRepo.RootFolder.Join(file.Filepath);
                    var targetFilepath = result.DiffConfig.GetTargetFilepath(srcFilepath);
                    var lineChanges = await gitRepo.GetLineChanges(srcSha, commit, srcFilepath).ConfigureAwait(false);
                    var lineChangesWithContext = await gitRepo.GetLineChangesWithContex(srcSha, commit, srcFilepath).ConfigureAwait(false);
                    var sourceText = await gitRepo.GetFileTextAtCommit(srcSha, srcFilepath).ConfigureAwait(false);
                    var targetText = await gitRepo.GetFileTextAtCommit(commit, targetFilepath).ConfigureAwait(false);
                    var (semanticDiff, unsemanticDiff) = DiffToMethods.SplitDiffByChanged(sourceText, targetText, file.SubsetOfMethodsThatAreNotEquivalent!, lineChanges, lineChangesWithContext);
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
                    break;
                case Status.ContainsSemanticChanges:
                case Status.NotCSharp:
                case Status.HasConditionalPreprocessor:
                case Status.ProjectDidNotCompile:
                case Status.Added:
                case Status.Removed:
                case Status.UnableToFindProj:
                case Status.ProjectDidNotRestore:
                case Status.UnexpectedError:
                default:
                    semanticChangesBuilder.Append(fullDiff);
                    break;
            }
        }

        var resultStr = await DisplayResults.GetPrettySummaryOfResultsAsync(result, gitRepo, "changes").ConfigureAwait(false);
        Logger.LogInformation(resultStr);

        var semanticFilepath = PatchFileLookup(CommitType.Behavioural);
        var unsemanticFilepath = PatchFileLookup(CommitType.Readability);
        if (semanticFilepath.Exists())
            File.Delete(semanticFilepath.Path);

        if (unsemanticFilepath.Exists())
            File.Delete(unsemanticFilepath.Path);

        var applyBuilder = new StringBuilder();

        if (semanticChangesBuilder.Length > 0)
        {
            await File.WriteAllTextAsync(semanticFilepath.Path, semanticChangesBuilder.ToString()).ConfigureAwait(false);
            Logger.LogInformation("Changes that DO effect runtime behaviour at: {semanticChanges}", semanticFilepath.Path);
            applyBuilder.AppendLine($"git apply {semanticFilepath.Path}");
        }

        if (unsemanticChangesBuilder.Length > 0)
        {
            await File.WriteAllTextAsync(unsemanticFilepath.Path, unsemanticChangesBuilder.ToString()).ConfigureAwait(false);
            Logger.LogInformation("Changes that do NOT effect runtime behaviour at: {UnsemanticChanges}", unsemanticFilepath.Path);
            applyBuilder.AppendLine($"git apply {unsemanticFilepath.Path}");
        }


        Logger.LogInformation("To apply:\n\n{ApplyBuilder}", applyBuilder.ToString());
    }


    private static async Task<(GitRepo ghostRepo, string baseCommit)> CreateGitRepoWithLocalChangesCommitted(AbsolutePath path, IncludeUncommittedChanges includeUncommittedChanges)
    {
        // Get a patch from the local version of the repo and grab the commit.
        // Setup a repo in the scratch space at the same base commit.
        // apply the patch.
        // Check the patch commit for semantic equality.
        var localChangesRepo = await GitRepo.SetupFromExistingFolder(path).ConfigureAwait(false);
        var baseCommit = await localChangesRepo.GetCurrentCommitSha().ConfigureAwait(false);

        var ghostRepo = await GitRepo.CreateGitRepoFromUrl(localChangesRepo.RemoteUrl).ConfigureAwait(false);
        var currentBaseCommit = await localChangesRepo.GetCurrentCommitSha().ConfigureAwait(false);
        await ghostRepo.Checkout(currentBaseCommit).ConfigureAwait(false);

        if (includeUncommittedChanges == IncludeUncommittedChanges.None) return (ghostRepo,baseCommit);

        var patchFilepath = ScratchSpacePath.Join($"tmp_{Guid.NewGuid()}.patch");

        var hasLocalChanges = await localChangesRepo.CreatePatchFileOfLocalChanges(patchFilepath, includeUncommittedChanges).ConfigureAwait(false);

        if (!hasLocalChanges) return (ghostRepo, baseCommit);

        await ghostRepo.ApplyPatch(patchFilepath).ConfigureAwait(false);
        await ghostRepo.AddAllAndCommit("All local changes").ConfigureAwait(false);

        return (ghostRepo,baseCommit);
    }

    public static async Task Commit(AbsolutePath repoPath, CommitType commitType, string message)
    {
        var bundlePath = ScratchSpacePath.Join($"{Guid.NewGuid()}.bundle");
        Logger.LogInformation("Creating Bundle at {Path}", bundlePath.Path);

        var userRepo = await GitRepo.SetupFromExistingFolder(repoPath);
        var userRepoBaseCommit = await userRepo.GetCurrentCommitSha().ConfigureAwait(false);
        // Get the ghost repo in a state that matches the users repo
        var ghostRepo = await GitRepo.CreateGitRepoFromUrl(userRepo.RemoteUrl).ConfigureAwait(false);
        await ghostRepo.Checkout(userRepoBaseCommit).ConfigureAwait(false);
        await ghostRepo.ApplyPatch(PatchFileLookup(commitType));
        await ghostRepo.AddAllAndCommit(message);
        
        // Now transfer the commit from the ghost repo back to the users repo.
        var newCommitSha = await ghostRepo.GetCurrentCommitSha().ConfigureAwait(false);
        await ghostRepo.CreateBundleFile(bundlePath);
        Logger.LogInformation("Applying Bundle to local repository, HEAD will be moved to {Sha}", newCommitSha);
        await userRepo.FetchBundle(bundlePath);
        await userRepo.Reset(newCommitSha);
    }

    private static AbsolutePath PatchFileLookup(CommitType commitType)
    {
        return commitType switch
        {
            CommitType.Behavioural => ScratchSpacePath.Join("change_behaviour.patch"),
            CommitType.Readability => ScratchSpacePath.Join("improve_readability.patch"),
            _ => throw new ArgumentOutOfRangeException(nameof(commitType), commitType, null)
        };
    }
}