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

    internal static async Task<bool> Run(string repoPathOrUrl, string target, string baseCommit,
        AbsolutePath? analyzerConfigPath, string? relativeProjFilter, AbsolutePath? explicitProjectFileMap,
        bool failFast)
    {
        GitRepo gitRepo;
        if (Directory.Exists(repoPathOrUrl))
        {
            var localChangesRepo = await GitRepo.SetupFromExistingFolder(new AbsolutePath(repoPathOrUrl)).ConfigureAwait(false);
            gitRepo = await GitRepo.CreateGitRepoFromUrl(localChangesRepo.RemoteUrl).ConfigureAwait(false);
        }
        else
        {
            gitRepo = await GitRepo.CreateGitRepoFromUrl(repoPathOrUrl).ConfigureAwait(false);
        }

        if (!await gitRepo.DoesBranchExist(target))
        {
            throw new ArgumentException($"target ({target}) not found in repo {gitRepo.RemoteUrl}");
        }
        
        if (!await gitRepo.DoesBranchExist(baseCommit))
        {
            throw new ArgumentException($"base ({baseCommit}) not found in repo {gitRepo.RemoteUrl}");
        }

        await gitRepo.AssertClean().ConfigureAwait(false);

        await gitRepo.CheckoutAndPull(target).ConfigureAwait(false);

        var projFilter = relativeProjFilter == null ? null : gitRepo.RootFolder.Join(relativeProjFilter);

        var commits = await gitRepo.ListCommitShasBetween(baseCommit, target).ConfigureAwait(false);

        var results = new List<CommitModel>();
        foreach (var c in commits)
        {
            Logger.LogInformation("Checking {C}", c);
            var result = await CheckSemanticEquivalence.CheckSemanticallyEquivalent(gitRepo, c, analyzerConfigPath, projFilter, explicitProjectFileMap, failFast).ConfigureAwait(false);
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

    internal static async Task<bool> RunAllAncestors(string repoPathOrUrl, string target,
        AbsolutePath? analyzerConfigPath,
        string? relativeProjFilter, AbsolutePath? projectMappingFilepath, AbsolutePath outputPath, bool failFast)
    {
        GitRepo gitRepo;
        if (File.Exists(repoPathOrUrl))
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
            var result = await CheckSemanticEquivalence.CheckSemanticallyEquivalent(gitRepo, c, analyzerConfigPath, projFilter, projectMappingFilepath, failFast)
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

    internal static async Task Split(string repoPath, string baseCommit, IncludeUncommittedChanges includeUncommitted, string? projectMap, bool failFast)
    {
        // Get a patch from the local version of the repo and grab the commit.
        // Setup a repo in the scratch space at the same base commit.
        // apply the patch.
        var userRepo = await GitRepo.SetupFromExistingFolder(new AbsolutePath(repoPath)).ConfigureAwait(false);
        
        if (!await userRepo.DoesBranchExist(baseCommit))
        {
            throw new ArgumentException($"base ({baseCommit}) not found in repo {userRepo.RemoteUrl}");
        }

        var userRepoBaseCommit = await userRepo.GetCurrentCommitSha().ConfigureAwait(false);
        var ghostRepo = await GitRepo.CreateGitRepoFromUrl(userRepo.RemoteUrl).ConfigureAwait(false);

        if (!await ghostRepo.DoesBranchExist(userRepoBaseCommit))
        {
            Logger.LogError("Unable to find current commit on remote repository. Please ensure you have run `git push` and try again. Exiting");
            return;
        }

        await ghostRepo.Checkout(userRepoBaseCommit).ConfigureAwait(false);

        if (includeUncommitted != IncludeUncommittedChanges.None)
        {
            var patchFilepath = ScratchSpacePath.Join($"tmp_{Guid.NewGuid()}.patch");

            var hasLocalChanges = await userRepo.CreatePatchFileOfLocalChanges(patchFilepath, includeUncommitted).ConfigureAwait(false);

            if (!hasLocalChanges && baseCommit == "HEAD")
            {
                Logger.LogInformation("No changes found. Exiting");
                return;
            }

            await ghostRepo.ApplyPatch(patchFilepath).ConfigureAwait(false);
            await ghostRepo.AddAllAndCommit("All local changes").ConfigureAwait(false);
        }

        if (baseCommit == "HEAD")
        {
            baseCommit = userRepoBaseCommit;
        }

        var projectMapPath = projectMap is null ? null : new AbsolutePath(projectMap);

        // Generate a patch so that we are only comparing a single commit.
        var patchText = await ghostRepo.Diff(baseCommit, "HEAD").ConfigureAwait(false);

        await SplitPatch(ghostRepo, baseCommit, patchText, projectMapPath, failFast).ConfigureAwait(false);
    }

    internal static async Task SplitRemote(string repoUrl, string target, string baseCommit, string? projectMap,
        bool failFast)
    {
        var gitRepo = await GitRepo.CreateGitRepoFromUrl(repoUrl).ConfigureAwait(false);

        if (!await gitRepo.DoesBranchExist(target))
        {
            throw new ArgumentException($"target ({target}) not found in repo {gitRepo.RemoteUrl}");
        }
        
        if (!await gitRepo.DoesBranchExist(baseCommit))
        {
            throw new ArgumentException($"base ({baseCommit}) not found in repo {gitRepo.RemoteUrl}");
        }

        baseCommit = await gitRepo.GetMergeBase(target, baseCommit).ConfigureAwait(false);

        // Generate a patch so that we are only comparing a single commit.
        var patchText = await gitRepo.Diff(baseCommit, target).ConfigureAwait(false);
        if (patchText.Length == 0)
        {
            Logger.LogInformation("No changes found. Exiting");
            return;
        }

        var projectMapPath = projectMap is null ? null : new AbsolutePath(projectMap);

        await SplitPatch(gitRepo, baseCommit, patchText, projectMapPath, failFast).ConfigureAwait(false);
    }

    private static async Task SplitPatch(GitRepo gitRepo, string baseCommit, string patchText,
        AbsolutePath? projectMapPath, bool failFast)
    {
        var patchFilepath = ScratchSpacePath.Join($"test-{Guid.NewGuid()}.patch");
        await File.WriteAllTextAsync(patchFilepath.Path, patchText).ConfigureAwait(false);

        await gitRepo.Checkout(baseCommit).ConfigureAwait(false);

        await gitRepo.ApplyPatch(patchFilepath).ConfigureAwait(false);
        await gitRepo.AddAllAndCommit("All local changes").ConfigureAwait(false);

        var commitWithPatch = await gitRepo.GetCurrentCommitSha().ConfigureAwait(false);

        await FindSplit(gitRepo, commitWithPatch, projectMapPath, failFast).ConfigureAwait(false);
    }

    private static async Task FindSplit(GitRepo gitRepo, string commit, AbsolutePath? projectMapPath, bool failFast)
    {
        var result = await CheckSemanticEquivalence
            .CheckSemanticallyEquivalent(gitRepo, commit, null, null, projectMappingFilepath: projectMapPath, failFast)
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
        var unsemanticFilepath = PatchFileLookup(CommitType.Quality);

        if (File.Exists(semanticFilepath.Path))
            File.Delete(semanticFilepath.Path);

        if (File.Exists(unsemanticFilepath.Path))
            File.Delete(unsemanticFilepath.Path);

        var applyBuilder = new StringBuilder();

        if (semanticChangesBuilder.Length > 0)
        {
            await File.WriteAllTextAsync(semanticFilepath.Path, semanticChangesBuilder.ToString()).ConfigureAwait(false);
            Logger.LogInformation("Changes that DO effect runtime behaviour at: {semanticChanges}", semanticFilepath.Path);
            applyBuilder.AppendLine(@"To apply:");
        }

        if (unsemanticChangesBuilder.Length > 0)
        {
            await File.WriteAllTextAsync(unsemanticFilepath.Path, unsemanticChangesBuilder.ToString()).ConfigureAwait(false);
            Logger.LogInformation("Changes that do NOT effect runtime behaviour at: {UnsemanticChanges}", unsemanticFilepath.Path);
            applyBuilder.AppendLine(@"To directly apply this as a commit use: semtex commit Behavioural ""<Commit message>""");

        }

        const string behaviouralCommand = @"semtex commit Behavioural ""<Commit message>""";
        const string qualityCommand = @"semtex commit Quality ""<Commit message>""";

        switch (semanticChangesBuilder.Length, unsemanticChangesBuilder.Length)
        {
            case ( > 0, > 0):
                Logger.LogInformation("To apply these changes as new (and separate) commits use the following commands:");
                Logger.LogInformation("");
                Logger.LogInformation(behaviouralCommand);
                Logger.LogInformation(qualityCommand);
                Logger.LogInformation("");
                Logger.LogInformation("If you wish to checkout the change set before commiting, stash your current changes and run: git apply <path>.patch");
                break;
            case ( > 0, 0):
                Logger.LogInformation("To apply this change as a new commit use the following command:");
                Logger.LogInformation("");
                Logger.LogInformation(behaviouralCommand);
                Logger.LogInformation("");
                Logger.LogInformation("If you wish to checkout the change set before commiting, stash your current changes and run: git apply <path>.patch");
                break;
            case (0, > 0):
                Logger.LogInformation("To apply this change as a new commit use the following command:");
                Logger.LogInformation("");
                Logger.LogInformation(qualityCommand);
                Logger.LogInformation("");
                Logger.LogInformation("If you wish to checkout the change set before commiting, stash your current changes and run: git apply <path>.patch");
                break;
        }
    }


    public static async Task Commit(AbsolutePath repoPath, CommitType commitType, string message)
    {
        var bundlePath = ScratchSpacePath.Join($"{Guid.NewGuid()}.bundle");
        Logger.LogInformation("Creating Bundle at {Path}", bundlePath.Path);

        var userRepo = await GitRepo.SetupFromExistingFolder(repoPath);
        var userRepoBaseCommit = await userRepo.GetCurrentCommitSha().ConfigureAwait(false);
        var ghostRepo = await GitRepo.CreateGitRepoFromUrl(userRepo.RemoteUrl).ConfigureAwait(false);

        if (!await ghostRepo.IsAvailable(userRepoBaseCommit))
        {
            Logger.LogError("Unable to find current commit on remote repository. Please ensure you have run `git push` and try again. Exiting");
            return;
        }

        // Get the ghost repo in a state that matches the users repo
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
            CommitType.Quality => ScratchSpacePath.Join("improve_quality.patch"),
            _ => throw new ArgumentOutOfRangeException(nameof(commitType), commitType, null)
        };
    }
}