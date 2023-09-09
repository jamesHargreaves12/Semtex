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
#if NETCOREAPP3_1
// This is a hack to make stuff work - kill once netcoreapp3.1 no longer supported
class PriorityQueue<T,T2>
{
    private readonly SortedDictionary<int, Queue<T>> _dictionary = new SortedDictionary<int, Queue<T>>();

    public void Enqueue(T item, int priority)
    {
        if (!_dictionary.ContainsKey(priority))
        {
            _dictionary[priority] = new Queue<T>();
        }

        _dictionary[priority].Enqueue(item);
    }

    public (T,int) Dequeue()
    {
        if (_dictionary.Count == 0)
        {
            throw new InvalidOperationException("The priority queue is empty.");
        }

        var (key, highestPriorityQueue) = _dictionary.First();
        T item = highestPriorityQueue.Dequeue();

        if (highestPriorityQueue.Count == 0)
        {
            _dictionary.Remove(_dictionary.First().Key);
        }

        return (item, key);
    }

    public bool TryDequeue(out T? value, out int key)
    {
        if (IsEmpty())
        {
            value = default(T);
            key = default(int);
            return false;
        }

        (value, key) = Dequeue();
        return true;
    }

    public bool IsEmpty()
    {
        return _dictionary.Count == 0;
    }
}
#endif
public sealed class CheckSemanticEquivalence
{
    private static readonly ILogger<CheckSemanticEquivalence> Logger = SemtexLog.LoggerFactory.CreateLogger<CheckSemanticEquivalence>();

    internal static async Task<CommitModel> CheckSemanticallyEquivalent(GitRepo gitRepo, string target,
        AbsolutePath? analyzerConfigPath, AbsolutePath? projFilter, AbsolutePath? projectMappingFilepath, bool failFast)
    {
        var stopWatch = Stopwatch.StartNew();
        var source = $"{target}~1";
        Logger.LogInformation("Processing git metadata...");
        // Use git diff to give us the set of files to consider.
        var diffConfig = await GetDiffConfig(gitRepo, target, source).ConfigureAwait(false);
        var fileModels = await CheckSemanticallyEquivalentInternal(gitRepo, diffConfig, source, target, analyzerConfigPath, projFilter, projectMappingFilepath, failFast).ConfigureAwait(false);
        return new CommitModel(target, fileModels, stopWatch.ElapsedMilliseconds, diffConfig)
        {
            CommitHash = target
        };

    }

    internal static async Task<List<FileModel>> CheckSemanticallyEquivalentInternal(GitRepo gitRepo, DiffConfig diffConfig, string source, string target,
        AbsolutePath? analyzerConfigPath, AbsolutePath? projFilter, AbsolutePath? projectMappingFilepath, bool failFast)
    {
        if (diffConfig.SourceCsFilepaths.Count == 0)
        {
            Logger.LogDebug("Skipping commit {Target} as it has no c# diffs", target);
            return await GetFileModels(gitRepo, diffConfig, SimplifiedSolutionSummary.Empty(), SimplifiedSolutionSummary.Empty(), new Dictionary<AbsolutePath, HashSet<MethodIdentifier>>())
                .ConfigureAwait(false);
        }

        Logger.LogDebug("{Count} C# files need to be checked for differences", diffConfig.SourceCsFilepaths.Count);

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

            var sourceChangedMethodsMap = new Dictionary<AbsolutePath, HashSet<MethodIdentifier>>();
            var targetChangedMethodsMap = new Dictionary<AbsolutePath, HashSet<MethodIdentifier>>();
            foreach (var key in sourceChangedMethods.Keys)
            {
                var targetKey = diffConfig.GetTargetFilepath(key);
                if (!targetChangedMethods.ContainsKey(targetKey)) continue;
                var toCheck = targetChangedMethods[targetKey].Union(sourceChangedMethods[key]).ToHashSet();
                sourceChangedMethodsMap[key] = toCheck;
                targetChangedMethodsMap[targetKey] = toCheck;
            }

            Logger.LogDebug("We have a method filter for {Percentage}% of methods ({ChangedCount} files)", (int)(sourceChangedMethodsMap.Count / (float)sourceFilesToSimplify.Count), sourceChangedMethodsMap.Count);

            // Setup the solutions
            Logger.LogInformation("Setting up LHS Solution...");
            await gitRepo.Checkout(target).ConfigureAwait(false);
            var targetProjFinder = GetProjFinder(gitRepo.RootFolder, projectMappingFilepath);
            var (targetSolution, targetUnsimplified, targetProjectIds) = await GetSolution(targetFilesToSimplify, projFilter, targetProjFinder).ConfigureAwait(false);

            Logger.LogInformation("Setting up RHS Solution...");
            await gitRepo.Checkout(source).ConfigureAwait(false);
            var sourceProjFinder = GetProjFinder(gitRepo.RootFolder, projectMappingFilepath);
            var (srcSolution, srcUnsimplified, sourceProjectIds) = await GetSolution(sourceFilesToSimplify, projFilter, sourceProjFinder).ConfigureAwait(false);

            if (sourceProjectIds.Count == 0 || targetProjectIds.Count == 0)
            {
                var srcSummary = new SimplifiedSolutionSummary(srcSolution, new HashSet<ProjectId>(), srcUnsimplified);
                var tgtSummary = new SimplifiedSolutionSummary(targetSolution, new HashSet<ProjectId>(), targetUnsimplified);
                return await GetFileModels(gitRepo, diffConfig, srcSummary, tgtSummary, sourceChangedMethods).ConfigureAwait(false);
            }

            sourceFilesToSimplify = sourceFilesToSimplify.Where(fp => !srcUnsimplified.IsUnsimplified(fp)).ToHashSet();
            targetFilesToSimplify = targetFilesToSimplify.Where(fp => !targetUnsimplified.IsUnsimplified(fp)).ToHashSet();

            Logger.LogInformation("Simplifying LHS...");
            // Simplify the solutions by apply rewriters and Analyzers/Code Fixes
            var simplifiedSrcSln = await SemanticSimplifier
                .GetSolutionWithFilesSimplified(srcSolution, sourceProjectIds, sourceFilesToSimplify, analyzerConfigPath, sourceChangedMethodsMap)
                .ConfigureAwait(false);

            Logger.LogInformation("Simplifying RHS...");
            var simplifiedTargetSln = await SemanticSimplifier
                .GetSolutionWithFilesSimplified(targetSolution, targetProjectIds, targetFilesToSimplify, analyzerConfigPath, targetChangedMethodsMap)
                .ConfigureAwait(false);

            // Co simplify here - some changes like renaming make much more sense to do once we have both left and right. Aim is to keep this set of operation small though.
            var filePathsToSimplify = diffConfig.SourceCsFilepaths
                .Select(p => (p, diffConfig.GetTargetFilepath(p)))
                .Where(pair =>
                    !srcUnsimplified.IsUnsimplified(pair.Item1)
                    && !targetUnsimplified.IsUnsimplified(pair.Item2)
                );

            (simplifiedSrcSln, simplifiedTargetSln) = await CoSimplifySolutions(simplifiedSrcSln, simplifiedTargetSln, sourceProjectIds, targetProjectIds, filePathsToSimplify).ConfigureAwait(false);

            var simplifiedSrcSolutionSummary = new SimplifiedSolutionSummary(simplifiedSrcSln, sourceProjectIds, srcUnsimplified);
            var simplifiedTargetSolutionSummary = new SimplifiedSolutionSummary(simplifiedTargetSln, targetProjectIds, targetUnsimplified);

            Logger.LogInformation("Calculating Equivalence...");
            var result = await GetFileModels(gitRepo, diffConfig, simplifiedSrcSolutionSummary, simplifiedTargetSolutionSummary, sourceChangedMethods).ConfigureAwait(false);
            // I am not sure why the GC is not smart enough to do this itself. But these lines prevent a linear increase in memory usage that just kills the process after a while.
            // Could it be the caching within Roslyn is holding references to some nodes which are then causing the reference to the wholue workspace to be held.  
            simplifiedSrcSln.Workspace.Dispose();
            simplifiedTargetSln.Workspace.Dispose();

            return result;
        }
        catch (SemtexCompileException e)
        {
            var commandline = GetReproCommandline(gitRepo, target, source, e.ProjectPath);
            Logger.LogWarning("Unexpected error: to reproduce the error use the following commandline args: \n {Commandline}",
                commandline);
            Logger.LogError(e.ToString());

            if (failFast)
            {
                throw;
            }

            var srcUnexpectErrorSummary = new UnsimplifiedFilesSummary(
                new HashSet<AbsolutePath>(),
                new HashSet<AbsolutePath>(),
                new HashSet<AbsolutePath>(),
                new HashSet<AbsolutePath>(),
                diffConfig.SourceCsFilepaths.ToHashSet()
            );
            var tgtUnexpectedErrorSummary = new UnsimplifiedFilesSummary(
                new HashSet<AbsolutePath>(),
                new HashSet<AbsolutePath>(),
                new HashSet<AbsolutePath>(),
                new HashSet<AbsolutePath>(),
                diffConfig.TargetCsFilepaths.ToHashSet()
            );
            return await GetFileModels(
                gitRepo,
                diffConfig,
                new SimplifiedSolutionSummary(null, new HashSet<ProjectId>(), srcUnexpectErrorSummary),
                new SimplifiedSolutionSummary(null, new HashSet<ProjectId>(), tgtUnexpectedErrorSummary),
                new Dictionary<AbsolutePath, HashSet<MethodIdentifier>>()
            ).ConfigureAwait(false);
        }

    }

    internal static async Task<(Solution srcSln, Solution targetSln)> CoSimplifySolutions(Solution srcSln, Solution targetSln, HashSet<ProjectId> sourceProjectIds, HashSet<ProjectId> targetProjectIds, IEnumerable<(AbsolutePath, AbsolutePath)> filepaths)
    {
        foreach (var (sourceFilepath, targetFilepath) in filepaths)
        {
            try
            {
                var sourceDoc = GetDocumentFromProjects(srcSln.Projects.Where(p => sourceProjectIds.Contains(p.Id)), sourceFilepath);
                var sourceRootNode = (await sourceDoc.GetSyntaxRootAsync().ConfigureAwait(false))!;
                var srcCompilation = await sourceDoc.Project.GetCompilationAsync().ConfigureAwait(false);
                var srcSemanticModel = srcCompilation!.GetSemanticModel(sourceRootNode.SyntaxTree);
                var sourceRenamablePrivateSymbolsWalker = new AllRenameablePrivateSymbols(srcSemanticModel);
                sourceRenamablePrivateSymbolsWalker.Visit(sourceRootNode);
                var srcRenamableSymbols = sourceRenamablePrivateSymbolsWalker.PrivateSymbols;

                var targetDoc = GetDocumentFromProjects(targetSln.Projects.Where(p => targetProjectIds.Contains(p.Id)), targetFilepath);
                var targetRootNode = (await targetDoc.GetSyntaxRootAsync().ConfigureAwait(false))!;
                var targetCompilation = await targetDoc.Project.GetCompilationAsync().ConfigureAwait(false);
                var targetSemanticModel = targetCompilation!.GetSemanticModel(targetRootNode.SyntaxTree);
                var targetRenamablePrivateSymbolsWalker = new AllRenameablePrivateSymbols(targetSemanticModel);
                targetRenamablePrivateSymbolsWalker.Visit(targetRootNode);
                var targetRenamableSymbols = targetRenamablePrivateSymbolsWalker.PrivateSymbols;

                sourceRootNode = new ConsistentOrderRewriter().Visit(sourceRootNode);
                srcSln = srcSln.WithDocumentSyntaxRoot(sourceDoc.Id, sourceRootNode);

                // By renaming the target we don't change the method identifiers of the source 
                var renameMapping = GetRenameMapping(targetRenamableSymbols, srcRenamableSymbols);
                targetRootNode = new RenameSymbolRewriter(targetSemanticModel, renameMapping).Visit(targetRootNode);
                targetRootNode = targetRootNode.NormalizeWhitespace();
                targetRootNode = new ConsistentOrderRewriter().Visit(targetRootNode);
                targetSln = targetSln.WithDocumentSyntaxRoot(targetDoc.Id, targetRootNode);
            }
            catch (CantFindDocumentException e)
            {
                Logger.LogDebug("Unable to find unique document in solution for Document at {path}", e.Path.Path);
                continue;
            }
        }

        return (srcSln, targetSln);
    }

    private static Dictionary<ISymbol, string> GetRenameMapping(HashSet<ISymbol> tgtPrivateVariables, HashSet<ISymbol> srcPrivateVariables)
    {
        // Could also use type of private variable and declaring type here but I think the assumption that renames will likely be closer than other renames is probably a good enough proxy.
        // O(N^2) complexity on the number of unmatched variables is fine - for almost all cases we expect the N to be small.
        var unmatchedTgtVariables = tgtPrivateVariables.Select(s => s.Name).ToHashSet().Except(srcPrivateVariables.Select(s => s.Name));
        var unmatchedSrcVariables = srcPrivateVariables.Select(s => s.Name).ToHashSet().Except(tgtPrivateVariables.Select(s => s.Name)).ToList();
        var queue = new PriorityQueue<(string, string), int>();
        foreach (var unmatchedTgtVariable in unmatchedTgtVariables)
        {
            foreach (var unmatchedSrcVariable in unmatchedSrcVariables)
            {
                queue.Enqueue((unmatchedTgtVariable, unmatchedSrcVariable), LevenshteinDistance.Calculate(unmatchedTgtVariable, unmatchedSrcVariable));
            }
        }

        var matchedTarget = new HashSet<string>();
        var matchedSource = new HashSet<string>();
        var mapping = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);
        while (queue.TryDequeue(out var pair, out var _))
        {
            var (tgt, src) = pair;
            if (matchedTarget.Contains(tgt) || matchedSource.Contains(src))
                continue;
            matchedTarget.Add(tgt);
            matchedSource.Add(src);
            mapping.Add(tgtPrivateVariables.First(x => x.Name == tgt), src);
        }

        return mapping;
    }

    private static string GetReproCommandline(GitRepo gitRepo, string target, string source, AbsolutePath? projectPath)
    {
        var projectFilter = projectPath is null ? "" : $"--project-filter \"{gitRepo.GetRelativePath(projectPath)}";
        var commandline =
            $"check {gitRepo.RemoteUrl} {target} --base {source} {projectFilter} --verbosity Debug\"";
        return commandline;
    }

    private static async Task<DiffConfig> GetDiffConfig(GitRepo gitRepo, string target, string source)
    {
        var (modifiedFilepaths, addedFilepaths, removedFilepaths, renamedFilepaths) =
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


    private static async Task<(Solution slnStart, UnsimplifiedFilesSummary, HashSet<ProjectId>)> GetSolution(
            HashSet<AbsolutePath> csFilepaths,
            AbsolutePath? projFilter,
            IProjFinder projFinder)
    {
        var filepathsWithIfPreprocessor = csFilepaths.Where(HasIfPreprocessor).ToHashSet();
        var filepathsToSimplify = csFilepaths.Except(filepathsWithIfPreprocessor).ToHashSet();
        var (projectToFilesMap, unableToFindProj) = projFinder.GetProjectToFileMapping(filepathsToSimplify, projFilter);

        var (slnStart, failedToRestore, failedToCompile) =
            await SolutionUtils.LoadSolution(projectToFilesMap.Keys.ToList()).ConfigureAwait(false);

        var filepathsInFailedToRestore = failedToRestore.SelectMany(f => projectToFilesMap[f]).ToHashSet();
        var filepathsInFailedToCompile = failedToCompile.SelectMany(f => projectToFilesMap[f]).ToHashSet();

        var projPathsToSimplify =
            projectToFilesMap.Keys.Where(p => projectToFilesMap[p].Count > 0 && !failedToRestore.Contains(p) && !failedToCompile.Contains(p)).ToHashSet();

        var projectsToSimplify = slnStart.Projects
            .Where(p => p.FilePath != null && projPathsToSimplify.Contains(new AbsolutePath(p.FilePath!)))
            .GroupBy(p => p.FilePath)
            .Select(g => SolutionUtils.GetHighestTargetVersion(g.ToList()))
            .ToList();
        var projectIdsToSimplify = projectsToSimplify.Select(p => p.Id).ToHashSet();

        // Because a single project can appear multiple times in a solution if it has multiple target frameworks then we should only pick one.
        // Even though we do a best effort attempt at removing multiple target versions above this can still occur due to the need to check dependencies.
        return (slnStart, new UnsimplifiedFilesSummary(filepathsWithIfPreprocessor, filepathsInFailedToCompile, unableToFindProj,
            filepathsInFailedToRestore, new HashSet<AbsolutePath>()), projectIdsToSimplify);
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
        Dictionary<AbsolutePath, HashSet<MethodIdentifier>> sourceChangedMethods)
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

            if (simplifiedTargetSolution.UnsimplifiedFilesSummary.FilepathsWithUnexpectedErrors.Contains(targetFilepath)
                || simplifiedSrcSolution.UnsimplifiedFilesSummary.FilepathsWithUnexpectedErrors.Contains(sourceFilepath))
            {
                fileResults.Add(new FileModel(relativePath, Status.UnexpectedError));
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


            Logger.LogDebug(SemtexLog.GetPerformanceStr(nameof(SemanticEqualBreakdown.GetSemanticallyUnequal), stopwatch.ElapsedMilliseconds));

            var result = semanticallyUnequal.Match(
                functions =>
                {
                    if (!functions.MethodIdentifiers.Any())
                        return new FileModel(relativePath, Status.SemanticallyEquivalent);

                    if (!sourceChangedMethods.ContainsKey(sourceFilepath) ||
                        functions.MethodIdentifiers.ToHashSet().IsProperSubsetOf(sourceChangedMethods[sourceFilepath]))
                    {
                        // We have limited the set of semantic changes to a smaller subset than the diff.
                        return new FileModel(relativePath, Status.SubsetOfDiffEquivalent, functions.MethodIdentifiers.ToHashSet());
                    }

                    if (!sourceChangedMethods.ContainsKey(sourceFilepath))
                        return new FileModel(relativePath, Status.ContainsSemanticChanges);

                    var newDiffs = functions.MethodIdentifiers.Where(x => !sourceChangedMethods[sourceFilepath].Contains(x));
                    // ReSharper disable once PossibleMultipleEnumeration
                    if (newDiffs.Count() != 0)
                    {
                        // ReSharper disable once PossibleMultipleEnumeration
                        Logger.LogDebug("Functions is not a subset of the input methods for file {Filepath}: {NewDiffs}", sourceFilepath, string.Join(",", newDiffs));
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
        if (simplifiedProjects is null)
            throw new CantFindDocumentException(filepath);

        switch (simplifiedProjects.SelectMany(p => p.Documents).Where(d => d.FilePath == filepath.Path).ToList())
        {
            case [var x]:
                return x;
            case []:
                Logger.LogDebug("Document not found in source projects {Path}", filepath.Path);
                throw new CantFindDocumentException(filepath);
            default:
                // This indicates that the same document is in multiple projects. Something that is not worth supporting.
                Logger.LogDebug("Document in multiple projects, reporting unable to find project {Path}",
                    filepath.Path);
                throw new CantFindDocumentException(filepath);
        }
    }
}