﻿using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Microsoft.Extensions.Logging;
using Semtex;
using Semtex.Logging;
using Semtex.Models;
// ReSharper disable ConvertClosureToMethodGroup

// Ew but seems to be the way return codes are done https://learn.microsoft.com/en-us/dotnet/standard/commandline/handle-termination
var returnCode = 0;
// This should be a tmp dir by default.
var homeDir = Environment.GetEnvironmentVariable("HOME");
#if DEBUG
var outputPath = $"{homeDir}/dev/Semtex/Semtex/Out";
var logPath = $"{outputPath}/Logs";
const bool shouldLogToFile = true;
#else
const bool shouldLogToFile = false;
var outputPath = Directory.GetCurrentDirectory();
var logPath = "";
#endif

// If true then any errors that are unhandled within the solution simplification will crash program. If false then the
// run will complete reporting the status for the C# files as UnexpectedError - Matters most in the case of --all-ancestors.
var failFast =
#if DEBUG
    true;
#else
    false;
#endif

var rootCommand = new RootCommand("Semtex - Remove the Git friction that is discouraging you from making improvements to your C# codebase");
rootCommand.AddCommand(GetSplitCommand());
rootCommand.AddCommand(GetSplitRemoteCommand());
rootCommand.AddCommand(GetCheckCommand());
rootCommand.AddCommand(CommitCommand());
rootCommand.AddCommand(GetComputeProjectMappingCommand());

await new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .Build()
    .InvokeAsync(args)
    .ConfigureAwait(false);
Thread.Sleep(50); // This ensures any logs come in before the next line of the terminal is rendered
return returnCode;

Command GetSplitCommand()
{
    var splitCommand = new Command("split", "Partition uncommitted changes into two patch files: one for behavioural changes and one for quality improvements.");
    var repoOption = new Option<string>("--repo-path", () => Directory.GetCurrentDirectory(), "Path to the local Git repository checkout. Accepts both absolute and relative paths.");
    var baseArg = new Option<string>("--base", () => "HEAD", "Branch or SHA to compare against.");
    var includeUncommittedOption = new Option<IncludeUncommittedChanges>("--include-uncommitted", () => IncludeUncommittedChanges.Staged, "Specify which uncommitted changes to include.");
    var projectMapOption = new Option<string?>("--project-map-file", "Use this option if your codebase does not follow the standard hierarchical project layout. Refer to the compute-project-file-map command for generating this file.");
    var verbosityOption = new Option<LogLevel>("--verbosity", () => LogLevel.Information, "Set the logging verbosity level.");
    splitCommand.AddOption(repoOption);
    splitCommand.AddOption(baseArg);
    splitCommand.AddOption(includeUncommittedOption);
    splitCommand.AddOption(verbosityOption);
    splitCommand.AddOption(projectMapOption);

    splitCommand.SetHandler(async (repoPath, baseValue, includeUncommited, projectMap, verbosity) =>
    {
        SemtexLog.InitializeLogging(verbosity, shouldLogToFile, logPath, verbosity == LogLevel.Information);
        await Commands.Split(repoPath, baseValue, includeUncommited, projectMap, failFast).ConfigureAwait(false);
    }, repoOption, baseArg, includeUncommittedOption, projectMapOption, verbosityOption);
    return splitCommand;
}

Command GetSplitRemoteCommand()
{
    var splitCommand = new Command("split-remote", "Split a specific commit from a remote repository into two patch files: one for behavioural changes and one for quality improvements.");
    var repoArg = new Argument<string>("repo-url", "Origin url of Git repository, including protocol (https, ssh, etc.).");
    var targetArg = new Argument<string>("target", "Branch or SHA containing the changes you want to split.");
    var baseOption = new Option<string?>("--base", "Specify the base branch or SHA for comparison. The default value is the parent commit of target.");
    var projectMapOption = new Option<string?>("--project-map-file", "Use this option if your codebase does not follow the standard hierarchical project layout. Refer to the compute-project-file-map command for generating this file.");
    var verbosityOption = new Option<LogLevel>("--verbosity", () => LogLevel.Information, "Set the logging verbosity level");
    splitCommand.AddArgument(repoArg);
    splitCommand.AddArgument(targetArg);
    splitCommand.AddOption(baseOption);
    splitCommand.AddOption(verbosityOption);
    splitCommand.AddOption(projectMapOption);

    splitCommand.SetHandler(async (repo, target, baseCommit, projectMap, verbosity) =>
    {
        SemtexLog.InitializeLogging(verbosity, shouldLogToFile, outputPath, verbosity == LogLevel.Information);
        await Commands.SplitRemote(repo, target, baseCommit ?? $"{target}~1", projectMap, failFast).ConfigureAwait(false);
    }, repoArg, targetArg, baseOption, projectMapOption, verbosityOption);
    return splitCommand;
}

Command GetCheckCommand()
{
    var checkCommand = new Command("check", "Analyze the specified remote repository and branch/commit to identify changes that will affect the program's runtime behaviour.");
    var repoArg = new Argument<string>("repo-url", "Origin url of Git repository, including protocol (https, ssh, etc.).");
    var targetArg = new Argument<string>("target", "Branch or SHA containing the changes.");
    var baseOption = new Option<string?>("--base", "Specify the base branch or SHA for comparison. The default value is the parent commit of target.");
    var projFilterOption = new Option<string?>("--project-filter", "Only consider changes in a specific project. Useful for debugging. ");
    var allAncestorsOption = new Option<bool>("--all-ancestors", () => false, "Analyze the previous 250 ancestors of the given commit. If this option is used, --base is ignored.");
    var projectMapOption = new Option<string?>("--project-map-file", "Use this option if your codebase does not follow the standard hierarchical project layout. Refer to the compute-project-file-map command for generating this file.");
    var verbosityOption = new Option<LogLevel>("--verbosity", () => LogLevel.Information, "Set the logging verbosity level.");
    checkCommand.AddArgument(repoArg);
    checkCommand.AddArgument(targetArg);
    checkCommand.AddOption(baseOption);
    checkCommand.AddOption(allAncestorsOption);
    checkCommand.AddOption(projFilterOption);
    checkCommand.AddOption(projectMapOption);
    checkCommand.AddOption(verbosityOption);
    checkCommand.SetHandler(async (repo, target, baseCommit, allAncestors, projFilter, explicitProjectMap, verbosity) =>
    {
        SemtexLog.InitializeLogging(verbosity, shouldLogToFile, outputPath, verbosity == LogLevel.Information);
        AbsolutePath? analyzerConfigPathTyped = null; // For now this is not supported
        var explicitProjectMapTyped = explicitProjectMap == null ? null : new AbsolutePath(explicitProjectMap);

        bool passedCheck;
        if (allAncestors)
        {
            if (baseCommit is not null)
                throw new ArgumentException("You can set both --all-ancestors and --base");
            passedCheck = await Commands.RunAllAncestors(repo, target, analyzerConfigPathTyped, projFilter, explicitProjectMapTyped, new AbsolutePath(outputPath), failFast).ConfigureAwait(false);
        }
        else
        {
            passedCheck = await Commands.Run(repo, target, baseCommit ?? $"{target}~1", analyzerConfigPathTyped, projFilter, explicitProjectMapTyped, failFast).ConfigureAwait(false);
        }

        if (!passedCheck)
        {
            returnCode = 1;
        }
    }, repoArg, targetArg, baseOption, allAncestorsOption, projFilterOption, projectMapOption, verbosityOption);
    return checkCommand;
}

Command GetComputeProjectMappingCommand()
{
    var projMappingCommand = new Command("compute-project-file-map", "This command is a prerequisite if your codebase does not follow the standard hierarchical project layout. It generates a file that maps each C# file to its respective project.");
    var slnPathArg = new Argument<string>("sln-path", "Path to local solution file. Accepts both absolute and relative paths.");
    var outPathArg = new Argument<string>("output-file-path", "Defines the path where the mapping will be saved.");
    var verbosityOption = new Option<LogLevel>("--verbosity", () => LogLevel.Information, "Set the logging verbosity level.");
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

Command CommitCommand()
{
    var commitCommand = new Command("commit", "Commits one of the partitions of your changes generated by the split commands");
    var typeArg = new Argument<CommitType>("type", "Whether to commit behavioural changes or quality improvements.");
    var repoOption = new Option<string>("--repo-path", () => Directory.GetCurrentDirectory(), "Path to the local Git repository checkout. Accepts both absolute and relative paths.");
    var messageArg = new Argument<string>("message", "Commit message");
    var verbosityOption = new Option<LogLevel>("--verbosity", () => LogLevel.Information, "Set the logging verbosity level.");
    commitCommand.AddArgument(typeArg);
    commitCommand.AddArgument(messageArg);
    commitCommand.AddOption(repoOption);
    commitCommand.AddOption(verbosityOption);
    commitCommand.SetHandler(async (repo, type, message, verbosity) =>
    {
        SemtexLog.InitializeLogging(verbosity, shouldLogToFile, outputPath, verbosity == LogLevel.Information);
        await Commands.Commit(new AbsolutePath(repo), type, message);
    }, repoOption, typeArg, messageArg, verbosityOption);
    return commitCommand;
}
