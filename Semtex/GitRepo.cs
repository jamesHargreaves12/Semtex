using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Logging;
using Semtex.Logging;
using Semtex.Models;

namespace Semtex;

internal record LineDiff(int Start, int Count)
{
    private int End => Start + Count;

    public bool Contains(LineDiff other)
    {
        return Start <= other.Start && End >= other.End;
    }
}

internal class GitRepo
{
    private static readonly ILogger<GitRepo> Logger = SemtexLog.LoggerFactory.CreateLogger<GitRepo>();
    public readonly AbsolutePath RootFolder;
    private static readonly string ScratchSpacePath = Path.Join(Path.GetTempPath(), "Semtex");

    public string RemoteUrl { get; }
    private static readonly Func<string, string> FormatOutputString = s => $"[git] {s}";
    // This is noisy for little gain. May regret this but right now its a pain.
    private static readonly PipeTarget StdOutPipe = PipeTarget.ToDelegate(s => Logger.LogDebug(FormatOutputString(s)));
    private static readonly PipeTarget StdErrPipe = PipeTarget.ToDelegate(s => Logger.LogDebug(FormatOutputString(s)));

    private GitRepo(AbsolutePath rootFolder, string remoteUrl)
    {
        RootFolder = rootFolder;
        RemoteUrl = remoteUrl;
    }

    private static async Task<AbsolutePath> GetRootFolder(AbsolutePath path)
    {
        var gitConfigCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "rev-parse",
                "--show-toplevel"
            })
            .WithWorkingDirectory(path.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitConfigCmd}", gitConfigCmd);
        var cmdResult = await gitConfigCmd.ExecuteBufferedAsync();
        Logger.LogDebug("Finished");
        return new AbsolutePath(cmdResult.StandardOutput.Replace("\n", ""));
    }

    public static async Task<GitRepo> SetupFromExistingFolder(AbsolutePath path)
    {
        var gitConfigCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "config",
                "--get", "remote.origin.url"
            })
            .WithWorkingDirectory(path.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitConfigCmd}", gitConfigCmd);
        var cmdResult = await gitConfigCmd.ExecuteBufferedAsync();
        Logger.LogDebug("Finished");
        var rootFolder = await GetRootFolder(path).ConfigureAwait(false);
        return new GitRepo(rootFolder, cmdResult.StandardOutput.Replace("\n", ""));
    }

    public async Task AssertClean()
    {
        var diff = await DiffUncommitted(true).ConfigureAwait(false)
                   + await DiffUncommitted(false).ConfigureAwait(false);
        if (diff.Any())
        {
            throw new Exception($"Expected {RootFolder} to be clean but it is not");
        }
    }

    public async Task<bool> CreatePatchFileOfLocalChanges(AbsolutePath patchFilepath, IncludeUncommittedChanges includeUncommittedChanges)
    {
        var patchText = await DiffUncommitted(includeUncommittedChanges == IncludeUncommittedChanges.Staged).ConfigureAwait(false);
        if (patchText.Length == 0)
        {
            return false;
        }
        Logger.LogDebug("Creating patch of current changes at {Path}", patchFilepath.Path);
        await File.WriteAllTextAsync(patchFilepath.Path, patchText).ConfigureAwait(false);
        return true;
    }


    public static async Task<GitRepo> CreateGitRepoFromUrl(string repoUrl)
    {
        // Clean / Create the temp directory for the build.
        var repoName = repoUrl.Split("/")[^1].Split(".")[0];
        var rootFolder = new AbsolutePath(Path.Join(ScratchSpacePath, repoName));

        if (Directory.Exists(rootFolder.Path))
        {
            Logger.LogDebug("Folder {RootFolder} already exists. Checking if it has the correct origin", rootFolder.Path);
            try
            {
                var existingRepo = await SetupFromExistingFolder(rootFolder).ConfigureAwait(false);
                if (existingRepo.RemoteUrl.Replace(".git", "") == repoUrl.Replace(".git", ""))
                {
                    await existingRepo.Fetch().ConfigureAwait(false);
                    if (await existingRepo.IsClean().ConfigureAwait(false))
                        return existingRepo;
                    Logger.LogWarning("Folder is not clean - it will be deleted and then checked out from scratch");
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Failed to load repo from existing folder with message {e}");
                // Will just continue and check it out from scratch.
            }
        }

        // Should be clever here and just check if it is the same repo and just git clean if so.
        Utils.EnsureDirectoryExistsAndEmpty(rootFolder);

        // Clone the repo into the temp directory 
        var gitRepo = await Clone(repoUrl, rootFolder).ConfigureAwait(false);
        return gitRepo;
    }

    private async Task<bool> IsClean()
    {
        var gitDiffCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "status",
                "--porcelain",
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitDiffCmd}", gitDiffCmd);
        var cmdResult = await gitDiffCmd.ExecuteBufferedAsync();
        Logger.LogDebug("Finished");
        return !cmdResult.StandardOutput.Any();
    }

    internal string GetRelativePath(AbsolutePath fullPath)
    {
        return RootFolder.GetRelativePath(fullPath);
    }

    internal static async Task<GitRepo> Clone(string repo, AbsolutePath rootFolder)
    {
        Logger.LogDebug("Cloning {Repo} at into {RootFolder}", repo, rootFolder.Path);
        var gitCloneCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "clone",
                repo,
                rootFolder.Path
            })
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitCloneCmd}", gitCloneCmd);
        await gitCloneCmd.ExecuteAsync();
        Logger.LogDebug("Finished Clone");
        return new GitRepo(rootFolder, repo);
    }

    internal async Task<(HashSet<AbsolutePath> modifiedFilepaths, HashSet<AbsolutePath> addedFilepaths, HashSet<AbsolutePath> removedFilepaths, HashSet<(AbsolutePath Source, AbsolutePath Target, int Similarity)> renamedFilepaths)> DiffFiles(string sourceSha, string targetSha)
    {
        var gitDiffCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "diff",
                $"{sourceSha}..{targetSha}",
                "--name-status",
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitDiffCmd}", gitDiffCmd);
        var cmdResult = await gitDiffCmd.ExecuteBufferedAsync();
        Logger.LogDebug("Finished");
        var diffResults = cmdResult.StandardOutput
            .Split("\n")
            .Where(c => !string.IsNullOrEmpty(c))
            .Select(c => c.Split("\t"))
            .ToList();
        var diffResultsLookup = diffResults
             .ToLookup(c => c[0]); // Renamings have a similarity score as well.


        // Not supporting "C", "T", "U", "X"
        var modifiedFilepaths = diffResultsLookup["M"]
            .Select(c => c[1])
            .Select(f => RootFolder.Join(f))
            .ToHashSet();
        var addedFilepaths = diffResultsLookup["A"]
            .Select(c => c[1])
            .Select(f => RootFolder.Join(f))
            .ToHashSet();
        var removedFilepaths = diffResultsLookup["D"]
            .Select(c => c[1])
            .Select(f => RootFolder.Join(f))
            .ToHashSet();
        var renamedFilepaths = diffResults
            .Where(c => c[0].StartsWith("R"))
            .Select(c => (RootFolder.Join(c[1]), RootFolder.Join(c[2]), int.Parse(c[0][1..])))
            .ToHashSet();
        // We should report the similarity because if they are R100 then we should not bother doing processing them.
        // However keeping them in for development is probably good as it means that we can assert nothing funny is going on.

        if (diffResultsLookup.Select(k => k.Count()).Sum() !=
            modifiedFilepaths.Count + addedFilepaths.Count + removedFilepaths.Count + renamedFilepaths.Count)
        {
            throw new Exception(
                $"Unhandled type of Diff - Types of diff: {string.Join(",", diffResultsLookup.Select(k => k.Key).ToList())}");
        }

        return (modifiedFilepaths, addedFilepaths, removedFilepaths, renamedFilepaths);
    }

    internal async Task Checkout(string sha)
    {
        var gitCheckoutCommand = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "-c", "advice.detachedHead=false",
                "checkout",
                sha
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitCheckoutCommand}", gitCheckoutCommand);
        await gitCheckoutCommand.ExecuteAsync();
        Logger.LogDebug("Finished Checkout");
    }

    internal async Task CheckoutAndPull(string shaOrBranch)
    {
        await Checkout(shaOrBranch).ConfigureAwait(false);
        // If you call pull on a detached head it will fail
        var branchName = await GetCurrentBranchName().ConfigureAwait(false);
        if (branchName == "HEAD")
        {
            return;
        }

        await Pull().ConfigureAwait(false);
    }

    internal async Task Fetch()
    {
        var gitFetchCommand = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "fetch",
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitFetchCommand}", gitFetchCommand);
        await gitFetchCommand.ExecuteAsync();
        Logger.LogDebug("Finished fetch");
    }

    internal async Task<List<string>> ListCommitShasBetween(string source, string target)
    {
        var gitLogCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "log",
                "--pretty=format:%H",
                target,
                $"^{source}"
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitLogCmd}", gitLogCmd);
        var cmdResult = await gitLogCmd.ExecuteBufferedAsync();
        Logger.LogDebug("Finished");
        return cmdResult.StandardOutput
            .Split("\n")
            .ToList();
    }

    internal async Task<List<string>> GetAllAncestors(string commitIdentifier, int limit = 250, bool withMerges = false)
    {
        var gitLogCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "log",
                "--pretty=format:%H",
                $"-n", limit.ToString(),
                withMerges ? "" : "--no-merges",
                commitIdentifier
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitLogCmd}", gitLogCmd);
        var cmdResult = await gitLogCmd.ExecuteBufferedAsync();
        Logger.LogDebug("Finished");
        return cmdResult.StandardOutput
            .Split("\n")
            .ToList();
    }

    internal async Task<string> GetCommitOnelineDisplay(string sha)
    {
        var gitLogCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "log",
                "-1", // 1 commit
                "--oneline", // <hash> <message>
                sha
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitLogCmd}", gitLogCmd);
        var cmdResult = await gitLogCmd.ExecuteBufferedAsync();
        Logger.LogDebug("Finished");
        return cmdResult.StandardOutput.Split("\n")[0];
    }

    internal async Task<string> GetCurrentCommitSha()
    {
        var gitLogCmd = Cli.Wrap("git").WithArguments(new[]
            {
                "log",
                "-1",
                "--pretty=format:%h"
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitLogCmd}", gitLogCmd);
        var cmdResult = await gitLogCmd.ExecuteBufferedAsync();
        Logger.LogDebug("Finished");
        return cmdResult.StandardOutput.Trim();
    }
    internal async Task<string> GetCurrentBranchName()
    {
        var gitLogCmd = Cli.Wrap("git").WithArguments(new[]
            {
                "rev-parse",
                "--abbrev-ref", "HEAD"
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitLogCmd}", gitLogCmd);
        var cmdResult = await gitLogCmd.ExecuteBufferedAsync();
        Logger.LogDebug("Finished");
        return cmdResult.StandardOutput.Replace("\n", "");
    }

    public async Task Pull()
    {
        var gitPullCommand = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "pull",
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitPullCommand}", gitPullCommand);
        await gitPullCommand.ExecuteAsync();
        Logger.LogDebug("Finished pull");
    }

    internal async Task<string> Diff(string ancestor, string target)
    {
        var gitDiffCommand = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "diff",
                ancestor,
                target
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitDiffCommand}", gitDiffCommand);
        var cmdResult = await gitDiffCommand.ExecuteBufferedAsync();
        Logger.LogDebug("Finished diff");
        return cmdResult.StandardOutput;
    }

    internal async Task<string> DiffUncommitted(bool stagedChanges)
    {
        var gitDiffCommand = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "diff",
                stagedChanges?"--cached":"",
            }.Where(x => x != "").ToArray())
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitDiffCommand}", gitDiffCommand);
        var cmdResult = await gitDiffCommand.ExecuteBufferedAsync();
        Logger.LogDebug("Finished diff");
        return cmdResult.StandardOutput;
    }

    public async Task ApplyPatch(AbsolutePath patchFilepath)
    {
        var gitDiffCommand = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "apply",
                patchFilepath.Path
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitDiffCommand}", gitDiffCommand);
        await gitDiffCommand.ExecuteAsync();
        Logger.LogDebug("Finished diff");
    }
    
    public async Task ApplyPatchToStaging(AbsolutePath patchFilepath)
    {
        var gitDiffCommand = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "apply",
                "--cached",
                patchFilepath.Path
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitDiffCommand}", gitDiffCommand);
        await gitDiffCommand.ExecuteAsync();
        Logger.LogDebug("Finished diff");
    }
    
    public async Task ApplyPatchToWorkingDir(AbsolutePath patchFilepath)
    {
        var gitDiffCommand = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "apply",
                "--cached",
                patchFilepath.Path
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitDiffCommand}", gitDiffCommand);
        await gitDiffCommand.ExecuteAsync();
        Logger.LogDebug("Finished diff");
    }

    public async Task AddAllAndCommit(string message)
    {
        var gitAddCommand = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "add",
                "."
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitAddCommand}", gitAddCommand);
        await gitAddCommand.ExecuteAsync();
        Logger.LogDebug("Finished diff");
        await Commit(message);

    }
    public async Task Commit(string message)
    {
        var gitCommitCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "commit",
                "-m", message
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitCommitCmd}", gitCommitCmd);
        await gitCommitCmd.ExecuteAsync();
        Logger.LogDebug("Finished diff");
    }

    public async Task<List<(LineDiff, LineDiff)>> GetLineChanges(string sourceSha, string targetSha, AbsolutePath sourceFileName)
    {
        var gitDiffCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "diff",
                $"{sourceSha}..{targetSha}",
                "--unified=0",
                "--", sourceFileName.Path
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(s =>
            {
                if (s.StartsWith("@@")) Logger.LogDebug(FormatOutputString(s)); // Only show the @@ lines as its to noisey otherwise
            }))
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitDiffCmd}", gitDiffCmd);
        var cmdResult = await gitDiffCmd.ExecuteBufferedAsync();
        var lines = cmdResult.StandardOutput
            .Split("\n");
        var result = new List<(LineDiff, LineDiff)>();
        var diffLines = lines
            .Select((line, i) => (line, i))
            .Where(pair => pair.line.StartsWith("@@"))
            .Select(pair => pair.i).ToList();

        foreach (var (start, _) in diffLines.Zip(diffLines.Skip(1).Concat(new[] { lines.Length })))
        {
            var parts = lines[start].Split(" ");
            var leftParts = parts[1];
            var rightPart = parts[2];
            result.Add((GetLineDiff(leftParts), GetLineDiff(rightPart)));
        }

        return result;
    }

    public async Task<List<(LineDiff, LineDiff, string)>> GetLineChangesWithContex(string sourceSha, string targetSha, AbsolutePath sourceFileName)
    {
        var gitDiffCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "diff",
                $"{sourceSha}..{targetSha}",
                "--", sourceFileName.Path
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(s =>
            {
                if (s.StartsWith("@@")) Logger.LogDebug(FormatOutputString(s)); // Only show the @@ lines as its to noisey otherwise
            }))
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitDiffCmd}", gitDiffCmd);
        var cmdResult = await gitDiffCmd.ExecuteBufferedAsync();
        var lines = cmdResult.StandardOutput
            .Split("\n");
        var result = new List<(LineDiff, LineDiff, string)>();
        var diffLines = lines
            .Select((line, i) => (line, i))
            .Where(pair => pair.line.StartsWith("@@"))
            .Select(pair => pair.i).ToList();

        foreach (var (start, end) in diffLines.Zip(diffLines.Skip(1).Concat(new[] { lines.Length })))
        {
            var parts = lines[start].Split(" ");
            var leftParts = parts[1];
            var rightPart = parts[2];
            result.Add((GetLineDiff(leftParts), GetLineDiff(rightPart), string.Join("\n", lines.Skip(start).Take(end - start))));
        }

        return result;
    }


    public async Task<string> GetFileDiff(string sourceSha, string targetSha, string relativeFilepath)
    {
        var gitDiffCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "diff",
                $"{sourceSha}..{targetSha}",
                "--", relativeFilepath
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitDiffCmd}", gitDiffCmd);
        var cmdResult = await gitDiffCmd.ExecuteBufferedAsync();
        Logger.LogDebug("Finished");
        return cmdResult.StandardOutput;
    }

    public async Task<string> GetMergeBase(string left, string right)
    {
        var gitMergeBaseCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "merge-base",
                left,
                right
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitCommitCmd}", gitMergeBaseCmd);
        var cmdResult = await gitMergeBaseCmd.ExecuteBufferedAsync();
        Logger.LogDebug("Finished diff");
        return cmdResult.StandardOutput.Trim();
    }

    private static LineDiff GetLineDiff(string gitDescriptionOfDiff)
    {
        var parts = gitDescriptionOfDiff.Replace("+", "").Replace("-", "").Split(",");
        return parts.Length switch
        {
            1 => new LineDiff(int.Parse(parts[0]), 1),
            2 => new LineDiff(int.Parse(parts[0]), int.Parse(parts[1])),
            _ => throw new ArgumentException(nameof(gitDescriptionOfDiff))
        };
    }

    internal async Task<string> GetFileTextAtCommit(string commit, AbsolutePath filepath)
    {
        var gitShowCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "show",
                $"{commit}:{GetRelativePath(filepath)}"
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitCommitCmd}", gitShowCmd);
        var cmdResult = await gitShowCmd.ExecuteBufferedAsync();
        Logger.LogDebug("Finished");
        return cmdResult.StandardOutput;

    }

    public async Task StashStaged()
    {
        var gitStashCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "stash",
                "save",
                "--staged",
                "--message",
                "Temporary Semtex"
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {GitCommitCmd}", gitStashCmd);
        await gitStashCmd.ExecuteAsync();

    }

    public async Task StashPop()
    {
        var gitStashCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "stash",
                "pop",
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {Cmd}", gitStashCmd);
        await gitStashCmd.ExecuteAsync();
    }

    public async Task CreateBundleFile(AbsolutePath bundlePath)
    {
        var gitBundleCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "bundle",
                "create",
                bundlePath.Path,
                "HEAD~1",
                "HEAD"
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {Cmd}", gitBundleCmd);
        await gitBundleCmd.ExecuteAsync();

    }

    internal async Task FetchBundle(AbsolutePath bundlePath)
    {
        var gitFetchCommand = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "fetch",
                bundlePath.Path
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {Cmd}", gitFetchCommand);
        await gitFetchCommand.ExecuteAsync();
    }

    public async Task Reset(string newCommitSha)
    {
        var gitResetCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "reset",
                newCommitSha
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogDebug("Executing {Cmd}", gitResetCmd);
        await gitResetCmd.ExecuteAsync();
    }
}