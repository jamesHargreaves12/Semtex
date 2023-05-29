using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Semtex.ProjectFinder;
using Semtex.Semantics;
using Microsoft.Extensions.Logging;
using Semtex.Logging;
using Semtex.Models;

namespace Semtex;

public class CheckSemanticEquivalence
{
    private static readonly ILogger<CheckSemanticEquivalence> Logger = SemtexLog.LoggerFactory.CreateLogger<CheckSemanticEquivalence>();

    internal static async Task<CommitModel> CheckSemanticallyEquivalent(GitRepo gitRepo, string target,
        string? analyzerConfigPath, string? projFilter, string? projectMappingFilepath)
    {
        var source = $"{target}~1";
        var stopWatch = Stopwatch.StartNew();
        // Use git diff to give us the set of files to consider.
        var diffConfig = await GetDiffConfig(gitRepo, target, source).ConfigureAwait(false);

        if (diffConfig.SourceCsFilepaths.Count == 0)
        {
            Logger.LogInformation("Skipping commit {Target} as it has no c# diffs",target);
            // Todo improve this early exit
            var fileModels = diffConfig.AllSourceFilePaths.Select(fp=>new FileModel(gitRepo.GetRelativePath(fp), Status.NotCSharp))
                .Concat(diffConfig.AddedFilepaths.Select(addedFp=> new FileModel(gitRepo.GetRelativePath(addedFp), Status.Added)))
                    .Concat(diffConfig.RemovedFilepaths.Select(removedFp=> new FileModel(gitRepo.GetRelativePath(removedFp), Status.Removed)))
                .ToList();
            return new CommitModel(target, fileModels, stopWatch.ElapsedMilliseconds)
            {
                CommitHash = target
            };
        }

        Logger.LogInformation("{Count} C# files need to be checked for differences", diffConfig.SourceCsFilepaths.Count);

        try
        {
            // TODO Really would be good to change type here or something to indicate that we are now dealing will full path rather than relative.
            // I think having a config class which we could pass around that offered both a relative and a full path could be useful
            var fullProjPath = projFilter is not null ? Path.Join(gitRepo.RootFolder, projFilter) : null;
            var sourceLineChangeMapping = new Dictionary<string, List<LineDiff>>();
            var targetLineChangeMapping = new Dictionary<string, List<LineDiff>>();
            foreach (var sourceFilepath in diffConfig.SourceCsFilepaths)
            {
                var lineChanges = await gitRepo.GetLineChanges(source, target, sourceFilepath).ConfigureAwait(false);
                sourceLineChangeMapping[sourceFilepath] = lineChanges.Select(l => l.Item1).ToList();
                
                var targetFilepath = diffConfig.GetTargetFilepath(sourceFilepath);
                targetLineChangeMapping[targetFilepath] = lineChanges.Select(l => l.Item2).ToList();
            }
            // TODO can this be done without an additional sourceChangedCheck
            await gitRepo.Checkout(target).ConfigureAwait(false);
            var targetChangedMethods =
                await GetChangesFilter(diffConfig.SourceCsFilepaths, targetLineChangeMapping).ConfigureAwait(false);
            
            await gitRepo.Checkout(source).ConfigureAwait(false);
            var sourceChangedMethods =
                await GetChangesFilter(diffConfig.SourceCsFilepaths, sourceLineChangeMapping).ConfigureAwait(false);

            var changedMethodsMap = new Dictionary<string, HashSet<string>>();
            foreach (var key in targetChangedMethods.Keys.Union(sourceChangedMethods.Keys))
            {
                if (!targetChangedMethods.ContainsKey(key)) continue;
                if(!sourceChangedMethods.ContainsKey(key))continue;
                changedMethodsMap[key] = targetChangedMethods[key].Union(sourceChangedMethods[key]).ToHashSet();
            }

            Logger.LogInformation("We have a method filter for {Percentage}% of methods ({ChangedCount} files)",(int)(changedMethodsMap.Count/(float)diffConfig.SourceCsFilepaths.Count), changedMethodsMap.Count);
            await gitRepo.Checkout(target).ConfigureAwait(false);
            var (targetSln, targetUnsimplifiedFiles) =
                await GetSimplifiedSolution(analyzerConfigPath, diffConfig.TargetCsFilepaths, fullProjPath, projectMappingFilepath, gitRepo.RootFolder, changedMethodsMap).ConfigureAwait(false);
            await gitRepo.Checkout(source).ConfigureAwait(false);
            var (sourceSln, sourceUnsimplifiedFiles) =
                await GetSimplifiedSolution(analyzerConfigPath, diffConfig.SourceCsFilepaths, fullProjPath, projectMappingFilepath, gitRepo.RootFolder, changedMethodsMap).ConfigureAwait(false);
            
            var result = await GetFileModels(gitRepo, diffConfig, targetUnsimplifiedFiles, sourceUnsimplifiedFiles, sourceSln, targetSln).ConfigureAwait(false);
            // I am not sure why the GC is not smart enough to do this itself. But these lines prevent a linear increase in memory usage that just kills the process after a while.
            // Could it be the caching within Roslyn is holding references to some nodes which are then causing the reference to the wholue workspace to be held.  
            sourceSln.Workspace.Dispose();
            targetSln.Workspace.Dispose();
            
            return new CommitModel(target, result, stopWatch.ElapsedMilliseconds)
            {
                CommitHash = target
            };
        }
        catch (SemtexCompileException e)
        {
            var commandline = $"check {gitRepo.RemoteUrl} {target} --source {source} --project-filter \"{gitRepo.GetRelativePath(e.ProjectPath)}\"";
            Logger.LogInformation("To reproduce the error use the following commandline args: \n {Commandline}", commandline);
            throw;
        }
        
    }
    
    private record DiffConfig
    {
        public DiffConfig(HashSet<string> addedFilepaths, HashSet<string> removedFilepaths, HashSet<(string Source, string Target)> renamedFilepaths, List<string> allSourceFilePaths, HashSet<string> sourceCsFilepaths, HashSet<string> targetCsFilepaths)
        {
            AddedFilepaths = addedFilepaths;
            RemovedFilepaths = removedFilepaths;
            RenamedFilepaths = renamedFilepaths;
            AllSourceFilePaths = allSourceFilePaths;
            SourceCsFilepaths = sourceCsFilepaths;
            TargetCsFilepaths = targetCsFilepaths;
        }

        internal HashSet<string> AddedFilepaths { get; }
        internal HashSet<string> RemovedFilepaths { get; }
        internal HashSet<(string Source, string Target)> RenamedFilepaths { get; }
        internal List<string> AllSourceFilePaths { get; }
        internal HashSet<string> SourceCsFilepaths { get; }
        internal HashSet<string> TargetCsFilepaths { get; }
        
        internal string GetTargetFilepath(string sourceFilepath)
        {
            string targetFilepath;
            if (RenamedFilepaths.Any(x => x.Source == sourceFilepath))
            {
                targetFilepath = RenamedFilepaths.First(x => x.Source == sourceFilepath).Target;
            }
            else
            {
                targetFilepath = sourceFilepath;
            }

            return targetFilepath;
        }

    }
    
    private static async Task<DiffConfig> GetDiffConfig(GitRepo gitRepo, string target, string source)
    {
       var( modifiedFilepaths, addedFilepaths, removedFilepaths, renamedFilepaths) =
            await gitRepo.DiffFiles(source, target).ConfigureAwait(false);

        var allSourceFilePaths = modifiedFilepaths
            .Concat(renamedFilepaths.Select(pair => pair.Source))
            .ToList();
        var sourceCsFilepaths = allSourceFilePaths
            .Where(f => f.EndsWith(".cs"))
            .ToHashSet();
        var targetCsFilepaths = modifiedFilepaths
            .Concat(renamedFilepaths.Select(pair => pair.Target))
            .Where(f => f.EndsWith(".cs"))
            .ToHashSet();


        return new DiffConfig(
            addedFilepaths, removedFilepaths, renamedFilepaths, allSourceFilePaths, sourceCsFilepaths,
            targetCsFilepaths);
    }
    

    private record UnsimplifiedFilesSummary
    {
        public UnsimplifiedFilesSummary(HashSet<string> filepathsWithIfPreprocessor, HashSet<string> filepathsInProjThatFailedToCompile, HashSet<string> filepathsWhichUnableToFindProjFor, HashSet<string> filepathsInProjThatFailedToRestore)
        {
            FilepathsWithIfPreprocessor = filepathsWithIfPreprocessor;
            FilepathsInProjThatFailedToCompile = filepathsInProjThatFailedToCompile;
            FilepathsWhichUnableToFindProjFor = filepathsWhichUnableToFindProjFor;
            FilepathsInProjThatFailedToRestore = filepathsInProjThatFailedToRestore;
        }

        internal HashSet<string> FilepathsWithIfPreprocessor { get; }
        internal HashSet<string> FilepathsInProjThatFailedToCompile { get; }
        internal HashSet<string> FilepathsWhichUnableToFindProjFor { get; }
        public HashSet<string> FilepathsInProjThatFailedToRestore { get; }
    }

    
    // I think this is better done through DI
    private static IProjFinder GetProjFinder(string rootFolder, string? explicitFilePath)
    {
        if (explicitFilePath is null) return new ClosestAncestorProjHeuristic();
        return new ExplicitFileMapToProj(explicitFilePath, rootFolder);
    }

    private static async Task<(Solution simplifiedSln, UnsimplifiedFilesSummary unsimplifiedFilesSummary)>
        GetSimplifiedSolution(string? analyzerConfigPath, HashSet<string> csFilepaths, string? projFilter,
            string? projectMappingFilepath, string rootFolder,
            Dictionary<string, HashSet<string>> changedMethodsMap)
    {
        var filepathsWithIfPreprocessor = csFilepaths.Where(HasIfPreprocessor).ToHashSet();
        var filepathsToSimplify = csFilepaths.Except(filepathsWithIfPreprocessor).ToHashSet();
        var (projectToFilesMap, unableToFindProj) = GetProjFinder(rootFolder, projectMappingFilepath).GetProjectToFileMapping(filepathsToSimplify, projFilter);
        
        var (slnStart,failedToRestore, failedToCompile) = await SolutionUtils.LoadSolution(projectToFilesMap.Keys.ToList()).ConfigureAwait(false);
        var filepathsInFailedToRestore = failedToRestore.SelectMany(f => projectToFilesMap[f]).ToHashSet();
        var filepathsInFailedToCompile = failedToCompile.SelectMany(f => projectToFilesMap[f]).ToHashSet();

        var projectIds = slnStart.Projects
            .Where(p => p.FilePath != null 
                        && projectToFilesMap.ContainsKey(p.FilePath)
                        && projectToFilesMap[p.FilePath].Count > 0
                        && !failedToRestore.Contains(p.FilePath)
                        && !failedToCompile.Contains(p.FilePath))
            .Select(p => p.Id)
            .ToList();

        var simplifiedSln = await SemanticSimplifier
                .GetSolutionWithFilesSimplified(slnStart, projectIds, projectToFilesMap, analyzerConfigPath, changedMethodsMap)
                .ConfigureAwait(false);
        return (simplifiedSln,
            new UnsimplifiedFilesSummary(filepathsWithIfPreprocessor, filepathsInFailedToCompile, unableToFindProj, filepathsInFailedToRestore));
    }
    

    // Conditional Preprocessors are difficult with in Roslyn and Roslynator doesn't always handle them correctly so we will just opt out of simplifying these files.
    // Another option would be to only the result of the compile that we applied and just treat anything else as trivia (like roslyn does) but that feels like something that should only be opt in.
    private static bool HasIfPreprocessor(string documentFilePath)
    {
        var file = new StreamReader(documentFilePath);
        while (file.ReadLine() is { } line)
        {
            if (line.Contains("#if"))
                return true;
        }

        return false;
    }
    
    // This needs a better name
    private static async Task<List<FileModel>> GetFileModels(GitRepo gitRepo, DiffConfig diffConfig,
        UnsimplifiedFilesSummary targetUnsimplified, UnsimplifiedFilesSummary sourceUnsimplified, Solution sourceSln,
        Solution targetSln)
    {
        Stopwatch? stopwatch = null;
        var fileResults = diffConfig.AddedFilepaths.Select(addedFp => new FileModel(gitRepo.GetRelativePath(addedFp), Status.Added))
            .Concat(diffConfig.RemovedFilepaths.Select(removedFp => new FileModel(gitRepo.GetRelativePath(removedFp), Status.Removed)))
            .ToList();
        foreach (var sourceFilepath in diffConfig.AllSourceFilePaths)
        {
            var relativePath = gitRepo.GetRelativePath(sourceFilepath);

            var targetFilepath = diffConfig.GetTargetFilepath(sourceFilepath);

            if (!diffConfig.SourceCsFilepaths.Contains(sourceFilepath) || !diffConfig.TargetCsFilepaths.Contains(targetFilepath))
            {
                fileResults.Add(new FileModel(relativePath, Status.NotCSharp));
                continue;
            }

            if (targetUnsimplified.FilepathsWhichUnableToFindProjFor.Contains(targetFilepath)
                || sourceUnsimplified.FilepathsWhichUnableToFindProjFor.Contains(sourceFilepath))
            {
                fileResults.Add(new FileModel(relativePath, Status.UnableToFindProj));
                continue;
            }

            if (targetUnsimplified.FilepathsInProjThatFailedToCompile.Contains(targetFilepath)
                || sourceUnsimplified.FilepathsInProjThatFailedToCompile.Contains(sourceFilepath))
            {
                fileResults.Add(new FileModel(relativePath, Status.ProjectDidNotCompile));
                continue;
            }

            if (targetUnsimplified.FilepathsWithIfPreprocessor.Contains(targetFilepath)
                || sourceUnsimplified.FilepathsWithIfPreprocessor.Contains(sourceFilepath))
            {
                fileResults.Add(new FileModel(relativePath, Status.HasConditionalPreprocessor));
                continue;
            }
            
            if (targetUnsimplified.FilepathsInProjThatFailedToRestore.Contains(targetFilepath)
                || sourceUnsimplified.FilepathsInProjThatFailedToRestore.Contains(sourceFilepath))
            {
                fileResults.Add(new FileModel(relativePath, Status.ProjectDidNotRestore));
                continue;
            }
            var before = sourceSln.Projects.SelectMany(p => p.Documents).Single(d => d.FilePath == sourceFilepath);
            var after = targetSln.Projects.SelectMany(p => p.Documents).Single(d => d.FilePath == targetFilepath);
            
            (stopwatch ??= new Stopwatch()).Restart();
            var areSemanticallyEqual =
                await SemanticsAwareEquality.SemanticallyEqual(before, after).ConfigureAwait(false);
            Logger.LogInformation(SemtexLog.GetPerformanceStr(nameof(SemanticsAwareEquality.SemanticallyEqual), stopwatch.ElapsedMilliseconds));
            
            fileResults.Add(areSemanticallyEqual
                ? new FileModel(relativePath, Status.SemanticallyEquivalent)
                : new FileModel(relativePath, Status.ContainsSemanticChanges));
        }

        return fileResults;
    }
    
        /// <summary>
    /// If all the changes in a function are within functions then we will only apply the analyzers that are located within those functions.
    /// TODO this does live here
    /// </summary>
    /// <param name="project"></param>
    /// <param name="filepaths"></param>
    /// <param name="lineChangeMapping"></param>
    /// <returns></returns>
    public static async Task<Dictionary<string, HashSet<string>>> GetChangesFilter(
        HashSet<string> filepaths, Dictionary<string, List<LineDiff>> lineChangeMapping)
    {
        Dictionary<string, HashSet<string>> result = new Dictionary<string, HashSet<string>>();
        foreach (var filepath in filepaths)
        {
            if (!lineChangeMapping.TryGetValue(filepath, out var lineDiffs)) continue;

            var fileText = await File.ReadAllTextAsync(filepath).ConfigureAwait(false);
            var fileLines = SourceText.From(fileText).Lines;

            var root = CSharpSyntaxTree.ParseText(fileText).GetRoot();
            var changedMethods = new HashSet<string>();
            var changeOutsideMethod = false;
            foreach (var lineDiff in lineDiffs)
            {
                var startI = Math.Max(0, lineDiff.Start - 1);
                var start = fileLines[startI].Start;
                var endI = Math.Max(lineDiff.Start + lineDiff.Count - 2,startI); //count is inclusive of first line, 0 indicates insert
                var end = fileLines[endI].EndIncludingLineBreak;
                var span = new TextSpan(start, end - start);
                var node = root!.FindNode(span);
                while (true)
                {
                    if (node is MethodDeclarationSyntax methodDeclarationSyntax) // need other cases here e.g. getters
                    {
                        changedMethods.Add(SemanticSimplifier.GetMethodIdentifier(methodDeclarationSyntax));
                        break;
                    }

                    if (node is ClassDeclarationSyntax or CompilationUnitSyntax or NamespaceDeclarationSyntax)
                    {
                        changeOutsideMethod = true;
                        break;
                    }

                    node = node.Parent;
                }

                if (changeOutsideMethod)
                {
                    break;
                }
            }

            if (!changeOutsideMethod)
            {
                result[filepath] = changedMethods;
            }

        }

        return result;
    }

}