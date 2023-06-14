using CliWrap;
using CliWrap.Buffered;
using CliWrap.Exceptions;
using Microsoft.Extensions.Logging;
using Roslynator.CSharp.Analysis;
using Semtex.Logging;
using Semtex.Models;

namespace Semtex;

public record LineDiff(int Start, int Count){}

internal class GitRepo
{
    private static readonly ILogger<GitRepo> Logger = SemtexLog.LoggerFactory.CreateLogger<GitRepo>();
    public readonly AbsolutePath RootFolder;
    private static readonly string ScratchSpacePath = Path.Join(Path.GetTempPath(), "Semtex");

    public string RemoteUrl { get; }
    private static Func<string,string> FormatOutputString = s => $"[git] {s}";
    private static readonly PipeTarget StdOutPipe = PipeTarget.ToDelegate(s => Logger.LogInformation(FormatOutputString(s)));
    private static readonly PipeTarget StdErrPipe = PipeTarget.ToDelegate(s => Logger.LogError(FormatOutputString(s)));

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
        Logger.LogInformation("Executing {GitConfigCmd}",gitConfigCmd);
        var cmdResult = await gitConfigCmd.ExecuteBufferedAsync();
        Logger.LogInformation("Finished");
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
        Logger.LogInformation("Executing {GitConfigCmd}",gitConfigCmd);
        var cmdResult = await gitConfigCmd.ExecuteBufferedAsync();
        Logger.LogInformation("Finished");
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
    
    public async Task<bool> CreatePatchFileOfLocalChanges(AbsolutePath patchFilepath)
    {
        var patchText = await DiffUncommitted(false).ConfigureAwait(false);
        if (patchText.Length == 0)
        {
            return false;
        }
        
        await File.WriteAllTextAsync(patchFilepath.Path, patchText).ConfigureAwait(false);
        return true;
    }
    
    
    public static async Task<GitRepo> CreateGitRepoFromUrl(string repoUrl)
    {
        // Clean / Create the temp directory for the build.
        var repoName = repoUrl.Split("/")[^1].Split(".")[0];
        var rootFolder = new AbsolutePath(Path.Join(ScratchSpacePath, repoName));

        if(Directory.Exists(rootFolder.Path))
        {
            Logger.LogInformation("Folder {RootFolder} already exists. Checking if it has the correct origin", rootFolder.Path);
            try
            {

                var existingRepo = await SetupFromExistingFolder(rootFolder).ConfigureAwait(false);
                // TODO Probably should do some clean here etc for sanity sake.
                if (existingRepo.RemoteUrl.Replace(".git","") == repoUrl.Replace(".git",""))
                {
                    await existingRepo.Fetch().ConfigureAwait(false);
                    return existingRepo;
                }
            }
            catch (CommandExecutionException e) // TODO exception type
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



    // TODO does this need to exist any more?
    internal string GetRelativePath(AbsolutePath fullPath)
    {
        return RootFolder.GetRelativePath(fullPath);
    }

    internal static async Task<GitRepo> Clone(string repo, AbsolutePath rootFolder)
    {
        Logger.LogInformation("Cloning {Repo} at into {RootFolder}", repo, rootFolder.Path);
        var gitCloneCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "clone",
                repo,
                rootFolder.Path
            })
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogInformation("Executing {GitCloneCmd}", gitCloneCmd);
        await gitCloneCmd.ExecuteAsync();
        Logger.LogInformation("Finished Clone");
        return new GitRepo(rootFolder, repo);
    }

    // TODO check what other statuses there are
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
        Logger.LogInformation("Executing {GitDiffCmd}",gitDiffCmd);
        var cmdResult = await gitDiffCmd.ExecuteBufferedAsync();
        Logger.LogInformation("Finished");
        var diffResults = cmdResult.StandardOutput
            .Split("\n")
            .Where(c => !string.IsNullOrEmpty(c))
            .Select(c => c.Split("\t"));
       var diffResultsLookup = diffResults
            .ToLookup(c => c[0]); // Renamings have a similarity score as well.

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
            .Where(c=>c[0].StartsWith("R"))
            .Select(c => (RootFolder.Join(c[1]),  RootFolder.Join(c[2]), int.Parse(c[0].Substring(1))))
            .ToHashSet();
        // We should report the similarity because if they are R100 then we should not bother doing processing them.
        // However keeping them in for development is probably good as it means that we can assert nothing funny is going on.

        if (diffResultsLookup.Select(k => k.Count()).Sum() !=
            modifiedFilepaths.Count + addedFilepaths.Count + removedFilepaths.Count + renamedFilepaths.Count)
        {
            throw new Exception(
                $"Unknown type of Diff - Types of diff: {string.Join(",", diffResultsLookup.Select(k => k.Key).ToList())}");
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
        Logger.LogInformation("Executing {GitCheckoutCommand}",gitCheckoutCommand);
        await gitCheckoutCommand.ExecuteAsync();
        Logger.LogInformation("Finished Checkout");
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
        Logger.LogInformation("Executing {GitFetchCommand}", gitFetchCommand);
        await gitFetchCommand.ExecuteAsync();
        Logger.LogInformation("Finished fetch");
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
        Logger.LogInformation("Executing {GitLogCmd}",gitLogCmd);
        var cmdResult = await gitLogCmd.ExecuteBufferedAsync();
        Logger.LogInformation("Finished");
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
        Logger.LogInformation("Executing {GitLogCmd}", gitLogCmd);
        var cmdResult = await gitLogCmd.ExecuteBufferedAsync();
        Logger.LogInformation("Finished");
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
        Logger.LogInformation("Executing {GitLogCmd}",gitLogCmd);
        var cmdResult = await gitLogCmd.ExecuteBufferedAsync();
        Logger.LogInformation("Finished");
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
        Logger.LogInformation("Executing {GitLogCmd}",gitLogCmd);
        var cmdResult = await gitLogCmd.ExecuteBufferedAsync();
        Logger.LogInformation("Finished");
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
        Logger.LogInformation("Executing {GitLogCmd}",gitLogCmd);
        var cmdResult = await gitLogCmd.ExecuteBufferedAsync();
        Logger.LogInformation("Finished");
        return cmdResult.StandardOutput.Replace("\n","");
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
        Logger.LogInformation("Executing {GitPullCommand}",gitPullCommand);
        await gitPullCommand.ExecuteAsync();
        Logger.LogInformation("Finished pull");
    }

    internal async Task<string> Diff(string left, string right)
    {
        var gitDiffCommand = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "diff",
                left, 
                right
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogInformation("Executing {GitDiffCommand}", gitDiffCommand);
        var cmdResult = await gitDiffCommand.ExecuteBufferedAsync();
        Logger.LogInformation("Finished diff");
        return cmdResult.StandardOutput;
    }

    internal async Task<string> DiffUncommitted(bool stagedChanges)
    {
        var gitDiffCommand = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "diff",
                stagedChanges?"--cached":"",
            }.Where(x=>x != "").ToArray())
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogInformation("Executing {GitDiffCommand}", gitDiffCommand);
        var cmdResult = await gitDiffCommand.ExecuteBufferedAsync();
        Logger.LogInformation("Finished diff");
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
        Logger.LogInformation("Executing {GitDiffCommand}", gitDiffCommand);
        await gitDiffCommand.ExecuteAsync();
        Logger.LogInformation("Finished diff");
    }

    public async Task AddAllAndCommit()
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
        Logger.LogInformation("Executing {GitAddCommand}", gitAddCommand);
        await gitAddCommand.ExecuteAsync();
        Logger.LogInformation("Finished diff");
        
        var gitCommitCmd = Cli.Wrap("git")
            .WithArguments(new[]
            {
                "commit",
                "-m", "local changes"
            })
            .WithWorkingDirectory(RootFolder.Path)
            .WithStandardOutputPipe(StdOutPipe)
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogInformation("Executing {GitCommitCmd}", gitCommitCmd);
        await gitCommitCmd.ExecuteAsync();
        Logger.LogInformation("Finished diff");
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
            .WithStandardOutputPipe(PipeTarget.ToDelegate(s=>
                {
                    if (s.StartsWith("@@")) Logger.LogInformation(FormatOutputString(s)); // Only show the @@ lines as its to noisey otherwise
                }))
            .WithStandardErrorPipe(StdErrPipe);
        Logger.LogInformation("Executing {GitDiffCmd}",gitDiffCmd);
        var cmdResult = await gitDiffCmd.ExecuteBufferedAsync();
        Logger.LogInformation("Finished");
        var diffLines = cmdResult.StandardOutput
            .Split("\n")
            .Where(line => line.StartsWith("@@"));
        var result = new List<(LineDiff,LineDiff)>();
        foreach (var line in diffLines)
        {
            var parts = line.Split(" ");
            var leftParts = parts[1];
            var rightPart = parts[2];
            result.Add((GetLineDiff(leftParts), GetLineDiff(rightPart)));
        }

        return result;
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
        Logger.LogInformation("Executing {GitCommitCmd}", gitMergeBaseCmd);
        var cmdResult = await gitMergeBaseCmd.ExecuteBufferedAsync();
        Logger.LogInformation("Finished diff");
        return cmdResult.StandardOutput.Trim();
    }

    private static LineDiff GetLineDiff(string gitDescriptionOfDiff)
    {
        var parts = gitDescriptionOfDiff.Replace("+","").Replace("-","").Split(",");
        if (parts.Length == 1)
        {
            return new LineDiff(int.Parse(parts[0]),1);
        }
        
        if(parts.Length == 2)
        {
            return new LineDiff(int.Parse(parts[0]), int.Parse(parts[1]));
        }

        throw new Exception("TODO");
    }
}