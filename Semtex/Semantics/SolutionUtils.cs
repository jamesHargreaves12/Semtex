using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;
using CliWrap.Exceptions;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using Semtex.Logging;

namespace Semtex.Semantics;

internal sealed class SolutionUtils
{
    private static readonly ILogger<SolutionUtils> Logger =
        SemtexLog.LoggerFactory.CreateLogger<SolutionUtils>();

    internal static async Task<Solution> LoadSolution(string slnPath)
    {
        try
        {
            await RunDotnetRestore(Path.GetDirectoryName(slnPath)!, Path.GetFileName(slnPath)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Logger.LogWarning($"dotnet restore failed on the solution with error {e}");
            Logger.LogWarning("This is a bad sign but there is very little we can do about it so lets continue");
        }

        var workspace = GetMsBuildWorkspace();
        await workspace.OpenSolutionAsync(slnPath).ConfigureAwait(false);
        return workspace.CurrentSolution;
    }
    
    
    internal static async Task<(Solution sln, List<string> failedToRestore, HashSet<string> failedToCompile)> LoadSolution(List<string> projectPaths)
    {
        var stopwatch = Stopwatch.StartNew();
        var (sln, failedToRestore) = await LoadSolutionImpl(projectPaths);
        Logger.LogInformation(SemtexLog.GetPerformanceStr(nameof(LoadSolutionImpl), stopwatch.ElapsedMilliseconds));

        stopwatch.Restart();
        var failedToCompile = await CheckProjectsCompile(sln, projectPaths);
        Logger.LogInformation(SemtexLog.GetPerformanceStr(nameof(CheckProjectsCompile), stopwatch.ElapsedMilliseconds));
        
        stopwatch.Restart();
        if (failedToCompile.Any())
        {
            Logger.LogWarning("The following projects failed initial compile: {ProjPaths}", string.Join("\n",failedToCompile));
            Logger.LogInformation("Will attempt to restore project and then try again");
            foreach (var path in failedToCompile)
            {
                await RunDotnetRestore(Path.GetDirectoryName(path)!,Path.GetFileName(path)).ConfigureAwait(false);
            }
            Logger.LogInformation("Reloading solution");
            (sln, var failedToRestore2) = await LoadSolutionImpl(projectPaths).ConfigureAwait(false);
            
            failedToRestore.AddRange(failedToRestore2);
            failedToCompile = await CheckProjectsCompile(sln, projectPaths);
            if (failedToCompile.Any())
            {
                Logger.LogWarning(
                    "The following projects are still failing to compile and so the files will not be checked: {ProjPaths}",
                    string.Join("\n", failedToCompile));
            }
            else
            {
                Logger.LogInformation("All projects now compiling");
            }
            Logger.LogInformation(SemtexLog.GetPerformanceStr(nameof(LoadSolution)+"-Reload", stopwatch.ElapsedMilliseconds));
        }

        return (sln, failedToRestore,failedToCompile);
    }

    private static async Task<(Solution sln, List<string>failedToRestore)> LoadSolutionImpl(List<string> projectPaths)
    {
        var failedToRestore = await RunDotnetRestoreOnAllProjects(projectPaths).ConfigureAwait(false);

        // TODO these first two lines are fairly slow. I assume it is the serial loading perhaps we should just create a very simple solution file upfront and then use that to load workspace in one go and assume that msft has optimized it under the hood?
        var workspace = GetMsBuildWorkspace();

        var sln = await LoadSolutionIntoWorkspace(workspace, projectPaths).ConfigureAwait(false);

        Logger.LogInformation("Loaded Solution with projects: {Projs}",
            string.Join(",", sln.Projects.Select(p => p.Name).ToList()));

        // If the project has WarnAsError we want to override this.
        foreach (var proj in sln.Projects)
        {
            if (proj.CompilationOptions is not null)
            {
                sln = sln.WithProjectCompilationOptions(proj.Id, proj.CompilationOptions!
                    .WithGeneralDiagnosticOption(ReportDiagnostic.Warn)
                    .WithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic>.Empty)
                );
                // SpecificDiagnosticOptions
            }
        }

        return (sln, failedToRestore);
    }

    internal static async Task<HashSet<string>> CheckProjectsCompile(Solution sln, List<string> projectPaths)
    {
        var failedToCompile = new HashSet<string>();
        var projectIds = sln.Projects
            .Where(p => p.FilePath != null && projectPaths.Contains(p.FilePath))
            .Select(p => p.Id)
            .ToList();
        foreach (var projId in projectIds) // This could 100% be parallelized for speed - for now won't do this as the logging becomes more difficult.
        {
            var proj = sln.GetProject(projId)!;
            Logger.LogInformation("Processing {ProjName}", proj.Name);

            // Confirm that there are no issues by compiling it once without analyzers.
            var compileStopWatch = Stopwatch.StartNew();
            var compilation = await proj.GetCompilationAsync().ConfigureAwait(false);
            var diagnostics = compilation!.GetDiagnostics();
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                Logger.LogError("Failed to compile before applying simplifications {Message}", diagnostics.First());
                failedToCompile.Add(proj.FilePath!);
            }

            compileStopWatch.Stop();
            Logger.LogInformation(SemtexLog.GetPerformanceStr("Initial Compilation", compileStopWatch.ElapsedMilliseconds));
        }

        return failedToCompile;
    }


    private static async Task<Solution> LoadSolutionIntoWorkspace(MSBuildWorkspace workspace, List<string> projectPaths)
    {
        var sw = Stopwatch.StartNew();
        foreach (var projPath in projectPaths)
        {
            if (workspace.CurrentSolution.Projects.Any(p => p.FilePath == projPath))
            {
                continue;
            }

            await workspace.OpenProjectAsync(projPath).ConfigureAwait(false);
        }

        var sln = workspace.CurrentSolution;
        Logger.LogInformation(SemtexLog.GetPerformanceStr("Load projects took:", sw.ElapsedMilliseconds));
        return sln;
    }
    
    private static MSBuildWorkspace GetMsBuildWorkspace()
    {
        void OnWorkspaceFailed(object? sender, WorkspaceDiagnosticEventArgs e)
        {

            if (e.Diagnostic.Message.EndsWith(
                    "was not recognized. It may be misspelled. If not, then the TargetFrameworkIdentifier and/or TargetFrameworkVersion properties must be specified explicitly."))
            {
                Logger.LogWarning(
                    "Miss-match framework error received. If this is due to multitargeting then it can be safely ignored");
                return;
            }

            Logger.LogError($"{nameof(OnWorkspaceFailed)}[{e.Diagnostic.Kind}] {e.Diagnostic.Message}");
        }

        // This call will update the MSBUILD_EXE_PATH and MSBuildSDKsPath Env Vars.
        if (!MSBuildLocator.IsRegistered) MSBuildLocator.RegisterDefaults();

        var frameworkVersion = MSBuildLocator.QueryVisualStudioInstances().FirstOrDefault();
        // Why would this help? 
        if (frameworkVersion is null)
        {
            MSBuildLocator.RegisterDefaults();
            frameworkVersion = MSBuildLocator.QueryVisualStudioInstances().FirstOrDefault();
        }

        var properties = frameworkVersion is not null? new Dictionary<string, string>
        {
        } : new Dictionary<string, string>();

        var workspace = MSBuildWorkspace.Create(properties);
        workspace.LoadMetadataForReferencedProjects = true;

        workspace.WorkspaceFailed += OnWorkspaceFailed;
        return workspace;
    }

    private static readonly HashSet<string> AlreadyRunDotNetRestoreOnProj = new();

    private static async Task<List<string>> RunDotnetRestoreOnAllProjects(List<string> projectPaths)
    {
        var failedToRestore = new List<string>() { };
        var sw = Stopwatch.StartNew();
        foreach (var path in projectPaths)
        {
            // just avoiding wasting time as we could do this every commit otherwise.
            // Sometimes we will need to rerun it. Not sure when this is - Could we diff the proj file to find out - doesn't help with dependent projs.?
            if (AlreadyRunDotNetRestoreOnProj.Contains(path))
            {
                continue;
            }

            try
            {
                var restoredFiles = await RunDotnetRestore(Path.GetDirectoryName(path)!,Path.GetFileName(path)).ConfigureAwait(false);
                if (!restoredFiles.Any())
                {
                    AlreadyRunDotNetRestoreOnProj.Add(path);
                    continue;
                }

                Logger.LogInformation("Restored {RestoredFilesFirst} and {OtherProjCount} other projects",restoredFiles[0], restoredFiles.Count-1);
                AlreadyRunDotNetRestoreOnProj.UnionWith(restoredFiles);

            }
            catch (CommandExecutionException)
            {
                failedToRestore.Add(path);
            }
        }

        Logger.LogInformation(SemtexLog.GetPerformanceStr(nameof(RunDotnetRestoreOnAllProjects), sw.ElapsedMilliseconds));
        return failedToRestore;
    }

    internal static async Task<List<string>> RunDotnetRestore(string location, string filename)
    {
        var dotnetRestoreCmd = Cli.Wrap("dotnet")
            .WithArguments(new[]
            {
                "restore", filename
            })
            .WithEnvironmentVariables(
                new
                    Dictionary<string, string?>() // calls to MSBuildLocator.RegisterDefaults will set these values. This causes an error if they do not match the relevant global.json.
                    {
                        { "MSBuildSDKsPath", null },
                        { "MSBUILD_EXE_PATH", null }
                    })
            .WithWorkingDirectory(location)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(s => Logger.LogInformation("[dotnet-restore] {S}", s)))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(s => Logger.LogError("[dotnet-restore] {S}", s)));
        Logger.LogInformation("Executing {DotnetRestoreCmd} @ {Location}", dotnetRestoreCmd, location);
        var cmdResult = await dotnetRestoreCmd.ExecuteBufferedAsync();
        Logger.LogInformation("Finished");
        return ExtractFilePaths(cmdResult.StandardOutput);
    }

    private static List<string> ExtractFilePaths(string text)
    {
        var regex = new Regex(@"\s(([A-Za-z]:)?(\/|\\)[^()]+\.csproj)");
        var matches = regex.Matches(text);
        var filePaths = new List<string>();

        foreach (Match match in matches)
        {
            filePaths.Add(match.Groups[1].Value);
        }

        return filePaths;
    }
}