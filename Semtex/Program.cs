using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Microsoft.Extensions.Logging;
using Semtex;
using Semtex.Logging;
using Semtex.Models;

// Ew but seems to be the way return codes are done https://learn.microsoft.com/en-us/dotnet/standard/commandline/handle-termination
var returnCode = 0;
// This should be a tmp dir by default.
var homeDir = Environment.GetEnvironmentVariable("HOME");
#if DEBUG
var outputPath = $"{homeDir}/dev/Semtex/Semtex/Out";
var logPath = $"{outputPath}/Logs";
var shouldLogToFile = true;
#else
var shouldLogToFile = false;
var outputPath = "";
var logPath = $"";
#endif

Command GetCheckCommand()
{
    var checkCommand = new Command("check");
    var repoArgument = new Argument<string>("repo", "repo url or path to your local checkout");
    var targetArgument = new Argument<string>("branch", "The commit hash or branch to be checked for semantic equivalence");
    var sourceOption = new Option<string>("--source", () => "origin/master", "What the target change set is checked against.");
    var allAncestorsOption = new Option<bool>("--all-ancestors", () => false, "Checks the previous 150 ancestors of a given commit");
    var projFilterOption = new Option<string?>("--project-filter", "Only consider changes in one project. Useful for debugging any issues.");
    var explicitProjectMapOption = new Option<string?>("--explicit-project-map-filepath", "Pass a file containing a map from document path to list of projects that that document is part of. If not passed then it is assumed that the document is a member of the closest project that is an ancestor.");
    // TODO Test this
    var analyzerConfigPathOption = new Option<string?>("--analyzer-config-path", "custom configuration to pass to the analyzers");
    var verbosityOption = new Option<LogLevel>("--verbosity", () => LogLevel.Information, "Set the logging verbosity level");
    checkCommand.AddArgument(repoArgument);
    checkCommand.AddArgument(targetArgument);
    checkCommand.AddOption(sourceOption);
    checkCommand.AddOption(allAncestorsOption);
    checkCommand.AddOption(analyzerConfigPathOption);
    checkCommand.AddOption(projFilterOption);
    checkCommand.AddOption(explicitProjectMapOption);
    checkCommand.AddOption(verbosityOption);
    checkCommand.SetHandler(async (repo, target, source, allAncestors, analyzerConfigPath, projFilter, explicitProjectMap, verbosity) =>
    {
        SemtexLog.InitializeLogging(verbosity, shouldLogToFile, outputPath, verbosity == LogLevel.Information);
        var analyzerConfigPathTyped = analyzerConfigPath == null ? null : new AbsolutePath(analyzerConfigPath);
        var explicitProjectMapTyped = explicitProjectMap == null ? null : new AbsolutePath(explicitProjectMap);

        bool passedCheck;
        if (allAncestors)
        {
            if (source is not "origin/master")
                throw new ArgumentException("You can set both --all-ancestors and --source");

            passedCheck = await Commands.RunAllAncestors(repo, target, analyzerConfigPathTyped, projFilter, explicitProjectMapTyped, new AbsolutePath(outputPath)).ConfigureAwait(false);
        }
        else
        {
            passedCheck = await Commands.Run(repo, target, source, analyzerConfigPathTyped, projFilter, explicitProjectMapTyped).ConfigureAwait(false);
        }

        if (!passedCheck)
        {
            returnCode = 1;
        }
    }, repoArgument, targetArgument, sourceOption, allAncestorsOption, analyzerConfigPathOption, projFilterOption, explicitProjectMapOption, verbosityOption);
    return checkCommand;
}

Command GetComputeProjectMappingCommand()
{
    var projMappingCommand = new Command("computeProjectFileMap");
    var slnPathArg = new Argument<string>("sln-path", "Path to .sln file");
    var outPathArg = new Argument<string>("output-file-path", "where to write the result");
    var verbosityOption = new Option<LogLevel>("--verbosity", () => LogLevel.Information, "Set the logging verbosity level");
    projMappingCommand.AddArgument(slnPathArg);
    projMappingCommand.AddArgument(outPathArg);
    projMappingCommand.AddOption(verbosityOption);
    projMappingCommand.SetHandler(async (slnPath, outPath, verbosity) =>
    {
        SemtexLog.InitializeLogging(verbosity, shouldLogToFile, outputPath, verbosity == LogLevel.Information);
        await Commands.ComputeProjectMapping(new AbsolutePath(slnPath), outPath).ConfigureAwait(false);
    }, slnPathArg, outPathArg, verbosityOption);
    return projMappingCommand;
}

// TODO test
Command GetSplitCommand()
{
    var splitCommand = new Command("split");
    var repoArg = new Argument<string>("repo_path", "local path");
    var baseArg = new Argument<string>("base", () => "HEAD", "branch or sha to compare against");
    var includeUncommittedOption = new Option<IncludeUncommittedChanges>("--includeUncommitted", () => IncludeUncommittedChanges.Staged, "Include uncommitted changes");
    var projectMapOption = new Option<string?>("--project-map", "Location of the file -> project mapping. See the computeProjectFileMap command for more information");
    var verbosityOption = new Option<LogLevel>("--verbosity", () => LogLevel.Information, "Set the logging verbosity level");
    splitCommand.AddArgument(repoArg);
    splitCommand.AddArgument(baseArg);
    splitCommand.AddOption(includeUncommittedOption);
    splitCommand.AddOption(projectMapOption);
    splitCommand.AddOption(verbosityOption);

    splitCommand.SetHandler(async (repoPath, baseValue, includeUncommited, projectMap, verbosity) =>
    {
        SemtexLog.InitializeLogging(verbosity, shouldLogToFile, logPath, verbosity == LogLevel.Information);
        await Commands.Split(repoPath, baseValue, includeUncommited, projectMap).ConfigureAwait(false);
    }, repoArg, baseArg, includeUncommittedOption, projectMapOption, verbosityOption);
    return splitCommand;
}

Command GetSplitRemoteCommand()
{
    var splitCommand = new Command("split_remote");
    var repoArg = new Argument<string>("repo_url", "url of repo");
    var targetArg = new Argument<string>("target", "branch or sha to compare against");
    var baseOption = new Option<string>("--base", () => "master", "base branch or sha");
    var projectMapOption = new Option<string?>("--project-map", "Location of the file -> project mapping. See the computeProjectFileMap command for more information");
    var verbosityOption = new Option<LogLevel>("--verbosity", () => LogLevel.Information, "Set the logging verbosity level");
    splitCommand.AddArgument(repoArg);
    splitCommand.AddArgument(targetArg);
    splitCommand.AddOption(baseOption);
    splitCommand.AddOption(projectMapOption);
    splitCommand.AddOption(verbosityOption);
    splitCommand.SetHandler(async (repo, target, baseCommit, projectMap, verbosity) =>
    {
        SemtexLog.InitializeLogging(verbosity, shouldLogToFile, outputPath, verbosity == LogLevel.Information);
        await Commands.SplitRemote(repo, target, baseCommit, projectMap).ConfigureAwait(false);
    }, repoArg, targetArg, baseOption, projectMapOption, verbosityOption);
    return splitCommand;
}


var rootCommand = new RootCommand("Semtex - Remove the Git friction that is discouraging you from making improvements to your C# codebase");
rootCommand.AddCommand(GetCheckCommand());
rootCommand.AddCommand(GetComputeProjectMappingCommand());
rootCommand.AddCommand(GetSplitCommand());
rootCommand.AddCommand(GetSplitRemoteCommand());
await new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .Build()
    .InvokeAsync(args)
    .ConfigureAwait(false);

return returnCode;