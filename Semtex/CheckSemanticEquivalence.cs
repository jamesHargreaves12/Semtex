using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Semtex.ProjectFinder;
using Semtex.Semantics;
using Microsoft.Extensions.Logging;
using OneOf;
using Semtex.Logging;
using Semtex.Models;
using Semtex.Rewriters;

namespace Semtex;

public sealed class CheckSemanticEquivalence
{
    private static readonly ILogger<CheckSemanticEquivalence> Logger = SemtexLog.LoggerFactory.CreateLogger<CheckSemanticEquivalence>();

    internal static async Task<CommitModel> CheckSemanticallyEquivalent(GitRepo gitRepo, string target,
        AbsolutePath? analyzerConfigPath, AbsolutePath? projFilter, AbsolutePath? projectMappingFilepath)
    {
        var source = $"{target}~1";
        var stopWatch = Stopwatch.StartNew();
        // Use git diff to give us the set of files to consider.
        var diffConfig = await GetDiffConfig(gitRepo, target, source).ConfigureAwait(false);

        if (diffConfig.SourceCsFilepaths.Count == 0)
        {
            Logger.LogInformation("Skipping commit {Target} as it has no c# diffs",target);
            var fileModels = await GetFileModels(gitRepo, diffConfig, SimplifiedSolutionSummary.Empty(), SimplifiedSolutionSummary.Empty(), new Dictionary<AbsolutePath, HashSet<string>>())
                .ConfigureAwait(false);

            return new CommitModel(target, fileModels, stopWatch.ElapsedMilliseconds, diffConfig)
            {
                CommitHash = target
            };
        }

        Logger.LogInformation("{Count} C# files need to be checked for differences", diffConfig.SourceCsFilepaths.Count);

        try
        {
            var sourceLineChangeMapping = new Dictionary<AbsolutePath, List<LineDiff>>();
            var targetLineChangeMapping = new Dictionary<AbsolutePath, List<LineDiff>>();
            // If the git diff is R100 then we don't need to bother simplifying as both sides will match.
            var sourceFilesToSimplify = diffConfig.SourceCsFilepaths
                .Where(sourceFilepath => !diffConfig.RenamedFilepaths.Any(renamedConfig => renamedConfig.Source == sourceFilepath && renamedConfig.Similarity == 100))
                .ToHashSet();
            var targetFilesToSimplify = sourceFilesToSimplify.Select(sourceFp => diffConfig.GetTargetFilepath(sourceFp))
                .ToHashSet();
            foreach (var sourceFilepath in sourceFilesToSimplify)
            {
                var lineChanges = await gitRepo.GetLineChanges(source, target, sourceFilepath).ConfigureAwait(false);
                sourceLineChangeMapping[sourceFilepath] = lineChanges.Select(l => l.Item1).ToList();
                
                var targetFilepath = diffConfig.GetTargetFilepath(sourceFilepath);
                targetLineChangeMapping[targetFilepath] = lineChanges.Select(l => l.Item2).ToList();
            }

            await gitRepo.Checkout(target).ConfigureAwait(false);
            var targetChangedMethods =
                    await DiffToMethods.GetChangesFilter(targetFilesToSimplify, targetLineChangeMapping).ConfigureAwait(false);
            
            await gitRepo.Checkout(source).ConfigureAwait(false);
            var sourceChangedMethods =
                await DiffToMethods.GetChangesFilter(sourceFilesToSimplify, sourceLineChangeMapping).ConfigureAwait(false);

            var sourceChangedMethodsMap = new Dictionary<AbsolutePath, HashSet<string>>();
            var targetChangedMethodsMap = new Dictionary<AbsolutePath, HashSet<string>>();
            foreach (var key in sourceChangedMethods.Keys)
            {
                var targetKey = diffConfig.GetTargetFilepath(key);
                if (!targetChangedMethods.ContainsKey(targetKey)) continue;
                var toCheck = targetChangedMethods[targetKey].Union(sourceChangedMethods[key]).ToHashSet();
                sourceChangedMethodsMap[key] = toCheck;
                targetChangedMethodsMap[targetKey] = toCheck;
            }

            Logger.LogInformation("We have a method filter for {Percentage}% of methods ({ChangedCount} files)",(int)(sourceChangedMethodsMap.Count/(float)sourceFilesToSimplify.Count), sourceChangedMethodsMap.Count);
            await gitRepo.Checkout(target).ConfigureAwait(false);
            var simplifiedTargetSolutionSummary = 
                await GetSimplifiedSolution(analyzerConfigPath, targetFilesToSimplify, projFilter, projectMappingFilepath, gitRepo.RootFolder, sourceChangedMethodsMap).ConfigureAwait(false);
            await gitRepo.Checkout(source).ConfigureAwait(false);
            var simplifiedSrcSolutionSummary =
                await GetSimplifiedSolution(analyzerConfigPath, sourceFilesToSimplify, projFilter, projectMappingFilepath, gitRepo.RootFolder, targetChangedMethodsMap).ConfigureAwait(false);
            
            // Co simplify here - some changes like renmaing make much more sense to do once we have both left and right. Aim is to keep this set of operation small though.
            var filePathsToSimplify = diffConfig.SourceCsFilepaths.Select(p => (p, diffConfig.GetTargetFilepath(p)));
            (simplifiedSrcSolutionSummary, simplifiedTargetSolutionSummary) = await CoSimplifySolutions(simplifiedSrcSolutionSummary, simplifiedTargetSolutionSummary, filePathsToSimplify).ConfigureAwait(false);
            
            var result = await GetFileModels(gitRepo, diffConfig, simplifiedSrcSolutionSummary, simplifiedTargetSolutionSummary, sourceChangedMethods).ConfigureAwait(false);
            // I am not sure why the GC is not smart enough to do this itself. But these lines prevent a linear increase in memory usage that just kills the process after a while.
            // Could it be the caching within Roslyn is holding references to some nodes which are then causing the reference to the wholue workspace to be held.  
            simplifiedTargetSolutionSummary.Sln?.Workspace.Dispose();
            simplifiedTargetSolutionSummary.Sln?.Workspace.Dispose();
            
            return new CommitModel(target, result, stopWatch.ElapsedMilliseconds, diffConfig)
            {
                CommitHash = target
            };
        }
        catch (SemtexCompileException e)
        {
            var commandline = GetReproCommandline(gitRepo, target, source, e.ProjectPath);
            Logger.LogInformation("To reproduce the error use the following commandline args: \n {Commandline}", commandline);
            throw;
        }
        
    }

    internal static async Task<(SimplifiedSolutionSummary simplifiedSrcSolutionSummary, SimplifiedSolutionSummary simplifiedTargetSolutionSummary)> CoSimplifySolutions(SimplifiedSolutionSummary srcSolutionSummary, SimplifiedSolutionSummary targetSolutionSummary, IEnumerable<(AbsolutePath, AbsolutePath)> filepaths)
    {
        var sourceProjectIds = srcSolutionSummary.SimplifiedProjectIds;
        var targetProjectIds = targetSolutionSummary.SimplifiedProjectIds;

        var sourceSln = srcSolutionSummary.Sln!;
        var targetSln = targetSolutionSummary.Sln!;
        foreach (var (sourceFilepath,targetFilepath) in filepaths)
        {
            if(srcSolutionSummary.UnsimplifiedFilesSummary.IsUnsimplified(sourceFilepath) || targetSolutionSummary.UnsimplifiedFilesSummary.IsUnsimplified(targetFilepath))
                continue;
            
            try
            {
                // Right now we get now advantage by doing this once we have both solutions in context TODO change this
                var sourceDoc = GetDocumentFromProjects(sourceSln.Projects.Where(p=>sourceProjectIds.Contains(p.Id)), sourceFilepath);
                var sourceRootNode = (await sourceDoc.GetSyntaxRootAsync().ConfigureAwait(false))!;
                var srcCompilation = await sourceDoc.Project.GetCompilationAsync().ConfigureAwait(false);
                var srcSemanticModel = srcCompilation!.GetSemanticModel(sourceRootNode.SyntaxTree);
                var sourceRenamablePrivateSymbolsWalker = new SemanticSimplifier.AllRenameablePrivateSymbols(srcSemanticModel);
                sourceRenamablePrivateSymbolsWalker.Visit(sourceRootNode);
                var srcRenamableSymbols = sourceRenamablePrivateSymbolsWalker.PrivateSymbols;
                
                sourceRootNode = new RenameSymbolRewriter(srcSemanticModel, srcRenamableSymbols).Visit(sourceRootNode);
                sourceRootNode = new ConsistentOrderRewriter().Visit(sourceRootNode);
                sourceSln = srcSolutionSummary.Sln!.WithDocumentSyntaxRoot(sourceDoc.Id, sourceRootNode);

                var targetDoc = GetDocumentFromProjects(targetSln.Projects.Where(p=>targetProjectIds.Contains(p.Id)), targetFilepath);
                var targetRootNode = (await targetDoc.GetSyntaxRootAsync().ConfigureAwait(false))!;
                var targetCompilation = await targetDoc.Project.GetCompilationAsync().ConfigureAwait(false);
                var targetSemanticModel = targetCompilation!.GetSemanticModel(targetRootNode.SyntaxTree);
                var targetRenamablePrivateSymbolsWalker = new SemanticSimplifier.AllRenameablePrivateSymbols(targetSemanticModel);
                targetRenamablePrivateSymbolsWalker.Visit(targetRootNode);
                var targetRenamableSymbols = targetRenamablePrivateSymbolsWalker.PrivateSymbols;
                targetRootNode = new RenameSymbolRewriter(targetSemanticModel, targetRenamableSymbols).Visit(targetRootNode);
                targetRootNode = new ConsistentOrderRewriter().Visit(targetRootNode);
                targetSln = targetSolutionSummary.Sln!.WithDocumentSyntaxRoot(targetDoc.Id, targetRootNode);


            }
            catch (CantFindDocumentException)
            {
                // TODO
                continue; 
            }
        }

        return (new SimplifiedSolutionSummary(sourceSln,sourceProjectIds, srcSolutionSummary.UnsimplifiedFilesSummary),
            new SimplifiedSolutionSummary(targetSln,targetProjectIds, targetSolutionSummary.UnsimplifiedFilesSummary));
    }

    private static string GetReproCommandline(GitRepo gitRepo, string target, string source, AbsolutePath? projectPath)
    {
        var projectFilter = projectPath is null ? "" : $"--project-filter \"{gitRepo.GetRelativePath(projectPath)}";
        var commandline =
            $"check {gitRepo.RemoteUrl} {target} --source {source} {projectFilter}\"";
        return commandline;
    }

    private static async Task<DiffConfig> GetDiffConfig(GitRepo gitRepo, string target, string source)
    {
       var( modifiedFilepaths, addedFilepaths, removedFilepaths, renamedFilepaths) =
            await gitRepo.DiffFiles(source, target).ConfigureAwait(false);

        var allSourceFilePaths = modifiedFilepaths
            .Concat(renamedFilepaths.Select(pair => pair.Source))
            .ToList();
        var sourceCsFilepaths = allSourceFilePaths
            .Where(f => f.Path.EndsWith(".cs"))
            .ToHashSet();
        var targetCsFilepaths = modifiedFilepaths
            .Concat(renamedFilepaths.Select(pair => pair.Target))
            .Where(f => f.Path.EndsWith(".cs"))
            .ToHashSet();


        return new DiffConfig(
            addedFilepaths, removedFilepaths, renamedFilepaths, allSourceFilePaths, sourceCsFilepaths,
            targetCsFilepaths, target, source);
    }


    // I think this is better done through DI
    private static IProjFinder GetProjFinder(AbsolutePath rootFolder, AbsolutePath? explicitFilePath)
    {
        if (explicitFilePath is null) return new ClosestAncestorProjHeuristic();
        return new ExplicitFileMapToProj(explicitFilePath, rootFolder);
    }
    

    private static async Task<SimplifiedSolutionSummary> GetSimplifiedSolution(
            AbsolutePath? analyzerConfigPath, 
            HashSet<AbsolutePath> csFilepaths, 
            AbsolutePath? projFilter,
            AbsolutePath? projectMappingFilepath, 
            AbsolutePath rootFolder,
            Dictionary<AbsolutePath, HashSet<string>> changedMethodsMap )
    {
        var filepathsWithIfPreprocessor = csFilepaths.Where(HasIfPreprocessor).ToHashSet();
        var filepathsToSimplify = csFilepaths.Except(filepathsWithIfPreprocessor).ToHashSet();
        var (projectToFilesMap, unableToFindProj) = GetProjFinder(rootFolder, projectMappingFilepath)
            .GetProjectToFileMapping(filepathsToSimplify, projFilter);

        var (slnStart, failedToRestore, failedToCompile) =
            await SolutionUtils.LoadSolution(projectToFilesMap.Keys.ToList()).ConfigureAwait(false);
        var projPathsToSimplify =
            projectToFilesMap.Keys.Where(p => projectToFilesMap[p].Count > 0 && !failedToRestore.Contains(p) && !failedToCompile.Contains(p)).ToHashSet();
        
        var filepathsInFailedToRestore = failedToRestore.SelectMany(f => projectToFilesMap[f]).ToHashSet();
        var filepathsInFailedToCompile = failedToCompile.SelectMany(f => projectToFilesMap[f]).ToHashSet();

        // Because a single project can appear multiple times in a solution if it has multiple target frameworks then we should only pick one.
        // Even though we do a best effort attempt at removing multiple target versions above this can still occur due to the need to check dependencies.
        var projectsToSimplify = slnStart.Projects
            .Where(p => p.FilePath != null && projPathsToSimplify.Contains(new AbsolutePath(p.FilePath!)))
            .GroupBy(p => p.FilePath)
            .Select(g => SolutionUtils.GetHighestTargetVersion(g.ToList()))
            .ToList();
        var simplifiedProjectIds = projectsToSimplify.Select(p => p.Id).ToHashSet();

        var simplifiedSln = await SemanticSimplifier
            .GetSolutionWithFilesSimplified(slnStart, simplifiedProjectIds, projectToFilesMap, analyzerConfigPath,
                changedMethodsMap)
            .ConfigureAwait(false);
        return new SimplifiedSolutionSummary(simplifiedSln, simplifiedProjectIds, new UnsimplifiedFilesSummary(filepathsWithIfPreprocessor, filepathsInFailedToCompile, unableToFindProj,
            filepathsInFailedToRestore));
    }


    // Conditional Preprocessors are difficult with in Roslyn and Roslynator doesn't always handle them correctly so we will just opt out of simplifying these files.
    // Another option would be to only the result of the compile that we applied and just treat anything else as trivia (like roslyn does) but that feels like something that should only be opt in.
    private static bool HasIfPreprocessor(AbsolutePath documentFilePath)
    {
        var file = new StreamReader(documentFilePath.Path);
        while (file.ReadLine() is { } line)
        {
            if (line.Contains("#if"))
                return true;
        }

        return false;
    }
    

    private static readonly HashSet<string> CommonSafeFilenames = new()
    {
        "README.md",
        "CONTRIBUTING.md",
        "LICENSE.md",
        "LICENSE.txt",
        "CHANGELOG.md",
        "ChangeLog.md",
        "Donations.md",
        "HISTORY.md",
        "CODE_OF_CONDUCT.md",
        "ISSUE_TEMPLATE.md",
        "PULL_REQUEST_TEMPLATE.md",
        "FAQ.md",
        "FAQ.txt",
        "TODO.md",
        "TODO.txt",
        "AUTHORS.md",
        "AUTHORS.txt",
        "INSTALL.md",
        "DEPRECATED.md",
        "CONFIGURATION.md",
        "CONFIGURATION.txt",
        "ROADMAP.md",
        "TESTS.md",
        "STYLEGUIDE.md",
        "CODING_STANDARDS.md",
        "SECURITY.md",
        "MAINTAINERS.md",
        "BUILDING.md",
        "API_REFERENCE.md",
        "GLOSSARY.md",
        "TROUBLESHOOTING.md",
        "DEPENDENCIES.md",
        ".editorconfig",
        ".gitignore"
    };

    private static readonly HashSet<string> CommonSafeFolders = new()
    {
        ".github/workflows"
    };

    private static bool IsKnownSafe(AbsolutePath path, GitRepo repo)
    {
        if (CommonSafeFilenames.Contains(Path.GetFileName(path.Path)))
            return true;

        var relativePath = repo.GetRelativePath(path);
        return CommonSafeFolders.Any(x => relativePath.StartsWith(x));
    }

    // This needs a better name
    private static async Task<List<FileModel>> GetFileModels(GitRepo gitRepo, DiffConfig diffConfig,
        SimplifiedSolutionSummary simplifiedSrcSolution, SimplifiedSolutionSummary simplifiedTargetSolution,
        Dictionary<AbsolutePath, HashSet<string>> sourceChangedMethods)
    {
        Stopwatch? stopwatch = null;
        var fileResults = diffConfig.AddedFilepaths.Select(addedFp => 
                new FileModel(gitRepo.GetRelativePath(addedFp), IsKnownSafe(addedFp, gitRepo) ? Status.SafeFile : Status.Added)
            )
            .Concat(diffConfig.RemovedFilepaths.Select(removedFp => 
                new FileModel(gitRepo.GetRelativePath(removedFp), IsKnownSafe(removedFp, gitRepo) ? Status.SafeFile : Status.Removed)))
            .ToList();
        foreach (var sourceFilepath in diffConfig.AllSourceFilePaths)
        {
            var relativePath = gitRepo.GetRelativePath(sourceFilepath);

            var targetFilepath = diffConfig.GetTargetFilepath(sourceFilepath);

            if (IsKnownSafe(sourceFilepath, gitRepo) && IsKnownSafe(targetFilepath, gitRepo))
            {
                fileResults.Add(new FileModel(relativePath, Status.SafeFile));
                continue;
            }

            if (!diffConfig.SourceCsFilepaths.Contains(sourceFilepath) ||
                !diffConfig.TargetCsFilepaths.Contains(targetFilepath))
            {
                fileResults.Add(new FileModel(relativePath, Status.NotCSharp));
                continue;
            }

            if (diffConfig.RenamedFilepaths.Any(
                    renamed => renamed.Source == sourceFilepath && renamed.Similarity == 100))
            {
                fileResults.Add(new FileModel(relativePath, Status.OnlyRename));
                continue;
            }

            if (simplifiedTargetSolution.UnsimplifiedFilesSummary.FilepathsWhichUnableToFindProjFor.Contains(targetFilepath)
                || simplifiedSrcSolution.UnsimplifiedFilesSummary.FilepathsWhichUnableToFindProjFor.Contains(sourceFilepath))
            {
                fileResults.Add(new FileModel(relativePath, Status.UnableToFindProj));
                continue;
            }

            if (simplifiedTargetSolution.UnsimplifiedFilesSummary.FilepathsInProjThatFailedToCompile.Contains(targetFilepath)
                || simplifiedSrcSolution.UnsimplifiedFilesSummary.FilepathsInProjThatFailedToCompile.Contains(sourceFilepath))
            {
                fileResults.Add(new FileModel(relativePath, Status.ProjectDidNotCompile));
                continue;
            }

            if (simplifiedTargetSolution.UnsimplifiedFilesSummary.FilepathsWithIfPreprocessor.Contains(targetFilepath)
                || simplifiedSrcSolution.UnsimplifiedFilesSummary.FilepathsWithIfPreprocessor.Contains(sourceFilepath))
            {
                fileResults.Add(new FileModel(relativePath, Status.HasConditionalPreprocessor));
                continue;
            }

            if (simplifiedTargetSolution.UnsimplifiedFilesSummary.FilepathsInProjThatFailedToRestore.Contains(targetFilepath)
                || simplifiedSrcSolution.UnsimplifiedFilesSummary.FilepathsInProjThatFailedToRestore.Contains(sourceFilepath))
            {
                fileResults.Add(new FileModel(relativePath, Status.ProjectDidNotRestore));
                continue;
            }

            Document sourceDoc;
            Document targetDoc;
            try
            {
                sourceDoc = GetDocumentFromProjects(simplifiedSrcSolution.SimplifiedProjects, sourceFilepath);
                targetDoc = GetDocumentFromProjects(simplifiedTargetSolution.SimplifiedProjects, targetFilepath);
            }
            catch (CantFindDocumentException)
            {
                fileResults.Add(new FileModel(relativePath, Status.UnableToFindProj));
                continue; 
            }
            
            (stopwatch ??= new Stopwatch()).Restart();

            OneOf<DifferencesLimitedToFunctions, CouldntLimitToFunctions> semanticallyUnequal;

            try
            {
                semanticallyUnequal = await SemanticEqualBreakdown.GetSemanticallyUnequal(sourceDoc, targetDoc).ConfigureAwait(false);
            }
            catch (Exception)
            {
                var projectPathRaw = sourceDoc.Project.FilePath;
                var projectPath = projectPathRaw is null ? null : new AbsolutePath(projectPathRaw);
                var commandline = GetReproCommandline(gitRepo, diffConfig.TargetSha, diffConfig.SourceSha, projectPath);
                Logger.LogInformation("To reproduce the error use the following commandline args: \n {Commandline}", commandline);
                throw;
            }


            Logger.LogInformation(SemtexLog.GetPerformanceStr(nameof(SemanticEqualBreakdown.GetSemanticallyUnequal), stopwatch.ElapsedMilliseconds));

            var result = semanticallyUnequal.Match(
                functions =>
                {
                    if (!functions.FunctionNames.Any())
                        return new FileModel(relativePath, Status.SemanticallyEquivalent);
                    
                    if (!sourceChangedMethods.ContainsKey(sourceFilepath) || 
                        functions.FunctionNames.ToHashSet().IsProperSubsetOf(sourceChangedMethods[sourceFilepath]))
                    {
                        // We have limited the set of semantic changes to a smaller subset than the diff.
                        return new FileModel(relativePath, Status.SubsetOfDiffEquivalent, functions.FunctionNames.ToHashSet());
                    }
                    
                    if(!sourceChangedMethods.ContainsKey(sourceFilepath))
                        return new FileModel(relativePath, Status.ContainsSemanticChanges);

                    var newDiffs = functions.FunctionNames.Where(x => !sourceChangedMethods[sourceFilepath].Contains(x));
                    // ReSharper disable once PossibleMultipleEnumeration
                    if (newDiffs.Count() != 0)
                    {
                        // ReSharper disable once PossibleMultipleEnumeration
                        Logger.LogWarning("Functions is not a subset of the input methods for file {Filepath}: {NewDiffs}", sourceFilepath, string.Join(",",newDiffs));
                    }
                    
                    return new FileModel(relativePath, Status.ContainsSemanticChanges);
                },
                _ => new FileModel(relativePath, Status.ContainsSemanticChanges));
            fileResults.Add(result);
        }

        return fileResults;
    }

    private static Document GetDocumentFromProjects(IEnumerable<Project>? simplifiedProjects, AbsolutePath filepath)
    {
        if(simplifiedProjects is null)
            throw new CantFindDocumentException();
        
        switch (simplifiedProjects.SelectMany(p => p.Documents).Where(d => d.FilePath == filepath.Path).ToList())
        {
            case [var x]:
                return x;
            case []:
                Logger.LogWarning("Document not found in source projects {Path}", filepath.Path);
                throw new CantFindDocumentException();
            default:
                // This indicates that the same document is in multiple projects. Something that is not worth supporting.
                Logger.LogWarning("Document in multiple projects, reporting unable to find project {Path}",
                    filepath.Path);
                throw new CantFindDocumentException();
        }
    }
}