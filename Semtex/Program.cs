using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Semtex;
using Semtex.Logging;
using Semtex.Models;

// Ew but seems to be the way return codes are done https://learn.microsoft.com/en-us/dotnet/standard/commandline/handle-termination
var returnCode = 0;
// This should be a tmp dir by default.
var homeDir = Environment.GetEnvironmentVariable("HOME");
var outputPath = $"{homeDir}/dev/Semtex/Semtex/Out";

Command GetCheckCommand(){
    var checkCommand = new Command("check");
    var repoArgument = new Argument<string>("repo", "repo url or path to your local checkout");
    var targetArgument = new Argument<string>("branch", "The commit hash or branch to be checked for semantic equivalence");
    var sourceOption = new Option<string>("--source", ()=> "origin/master","What the target change set is checked against.");
    var allAncestorsOption = new Option<bool>("--all-ancestors", ()=>false,"Checks the previous 150 ancestors of a given commit");
    var projFilterOption = new Option<string?>("--project-filter", "Only consider changes in one project. Useful for debugging any issues.");
    var explicitProjectMapOption = new Option<string?>("--explicit-project-map-filepath", "Pass a file containing a map from document path to list of projects that that document is part of. If not passed then it is assumed that the document is a member of the closest project that is an ancestor.");
    // TODO Test this
    var analyzerConfigPathOption = new Option<string?>("--analyzer-config-path","custom configuration to pass to the analyzers");
    checkCommand.AddArgument(repoArgument);
    checkCommand.AddArgument(targetArgument);
    checkCommand.AddOption(sourceOption);
    checkCommand.AddOption(allAncestorsOption);
    checkCommand.AddOption(analyzerConfigPathOption);
    checkCommand.AddOption(projFilterOption);
    checkCommand.AddOption(explicitProjectMapOption);
    checkCommand.SetHandler(async (repo, target, source, allAncestors, analyzerConfigPath, projFilter, explicitProjectMap) =>
    {
        SemtexLog.InitializeLogging(outputPath);
        var analyzerConfigPathTyped = analyzerConfigPath == null ? null : new AbsolutePath(analyzerConfigPath);
        var explicitProjectMapTyped = explicitProjectMap == null ? null : new AbsolutePath(explicitProjectMap);

        bool passedCheck;
        if (!allAncestors)
        {
            passedCheck = await Commands.Run(repo, target, source, analyzerConfigPathTyped, projFilter, explicitProjectMapTyped)
                .ConfigureAwait(false);
        }
        else
        {
            // TODO Check source hasn't been set by the user.
            passedCheck = await Commands.RunAllAncestors(repo, target, analyzerConfigPathTyped, projFilter, explicitProjectMapTyped, new AbsolutePath(outputPath))
                .ConfigureAwait(false);
        }

        if (!passedCheck)
        {
            returnCode = 1;
        }
    }, repoArgument, targetArgument, sourceOption, allAncestorsOption, analyzerConfigPathOption, projFilterOption,explicitProjectMapOption);
    return checkCommand;
}

Command GetCheckUncommittedCommand(){
    // TODO descriptions need work.
    var checkCommand = new Command("modified");
    var pathArgument = new Argument<string>("path", "path to local git repo");
    var stagedArgument = new Option<bool>("--staged", ()=> false ,"If true we will look at staged files");
    var projFilterOption = new Option<string?>("--project-filter", "Only consider changes in one project. Useful for debugging any issues.");
    var analyzerConfigPathOption = new Option<string?>("--analyzer-config-path","custom configuration to pass to the analyzers");
    var explicitProjectMapOption = new Option<string?>("--explicit-project-map-filepath", "Pass a file containing a map from document path to list of projects that that document is part of. If not passed then it is assumed that the document is a member of the closest project that is an ancestor.");
    
    checkCommand.AddArgument(pathArgument);
    checkCommand.AddOption(stagedArgument);
    checkCommand.AddOption(analyzerConfigPathOption);
    checkCommand.AddOption(projFilterOption);
    checkCommand.AddOption(explicitProjectMapOption);
    checkCommand.SetHandler(async (path, staged, analyzerConfigPath, projFilter, explicitProjectMap) =>
    {
        SemtexLog.InitializeLogging(outputPath);
        var analyzerConfigPathTyped = analyzerConfigPath == null ? null : new AbsolutePath(analyzerConfigPath);
        var pathTyped = new AbsolutePath(path);
        var projFilterTyped = new AbsolutePath(path);
        var explicitProjectMapTyped = new AbsolutePath(path);
        var passedCheck = await Commands.RunModified(pathTyped, analyzerConfigPathTyped, staged, projFilterTyped, explicitProjectMapTyped)
            .ConfigureAwait(false);

        if (!passedCheck)
        {
            returnCode = 1;
        }
    }, pathArgument, stagedArgument, analyzerConfigPathOption, projFilterOption, explicitProjectMapOption);
    return checkCommand;
}

Command GetComputeProjectMappingCommand()
{
    var projMappingCommand = new Command("computeProjectFileMap");
    var slnPathArg = new Argument<string>("sln-path", "Path to .sln file");
    var outPathArg = new Argument<string>("output-file-path", "where to write the result");
    projMappingCommand.AddArgument(slnPathArg);
    projMappingCommand.AddArgument(outPathArg);
    projMappingCommand.SetHandler(async (slnPath, outPath) =>
    {
        SemtexLog.InitializeLogging(outputPath);
        await Commands.ComputeProjectMapping(new AbsolutePath(slnPath), outPath).ConfigureAwait(false);
    }, slnPathArg, outPathArg);
    return projMappingCommand;
}


Command GetSplitCommand()
{
    var splitCommand = new Command("split");
    var repoArg = new Argument<string>("repo", "local path / url of repo");
    var sourceArg = new Argument<string>("source", "Branch or sha to compare against");
    var targetOption = new Option<string?>("--target", "Used to specify a different branch or sha to compare against.");
    var includeUncommittedOption = new Option<IncludeUncommittedChanges>("--includeUncommitted", ()=>IncludeUncommittedChanges.None, "Include uncommitted changes");
    var projectMapOption = new Option<string?>("--project-map", "Location of the file -> project mapping. See the computeProjectFileMap command for more information");
    splitCommand.AddArgument(repoArg);
    splitCommand.AddArgument(sourceArg);
    splitCommand.AddOption(targetOption);
    splitCommand.AddOption(includeUncommittedOption);
    splitCommand.AddOption(projectMapOption);
    splitCommand.SetHandler(async (repo, source, target, includeUncommited, projectMap) =>
    {
        SemtexLog.InitializeLogging(outputPath);
        await Commands.Split(repo, source, target, includeUncommited, projectMap).ConfigureAwait(false);
    }, repoArg, sourceArg, targetOption,includeUncommittedOption,projectMapOption);
    return splitCommand;
}

var rootCommand = new RootCommand("Semtex - Remove the Git friction that is discouraging you from making improvements to your C# codebase");
rootCommand.AddCommand(GetCheckCommand());
rootCommand.AddCommand(GetCheckUncommittedCommand());
rootCommand.AddCommand(GetComputeProjectMappingCommand());
rootCommand.AddCommand(GetSplitCommand());
await new CommandLineBuilder(rootCommand)
    .UseDefaults()
    .Build()
    .InvokeAsync(args)
    .ConfigureAwait(false);

return returnCode;