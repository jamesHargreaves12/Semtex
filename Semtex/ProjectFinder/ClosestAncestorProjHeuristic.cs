using Microsoft.Extensions.Logging;
using Semtex.Logging;
using Semtex.Models;

namespace Semtex.ProjectFinder;

// If we have other ways of deriving project mapping then then it probably makes sense to split this up
public sealed class ClosestAncestorProjHeuristic : IProjFinder
{
    private static readonly ILogger<ClosestAncestorProjHeuristic> Logger = SemtexLog.LoggerFactory.CreateLogger<ClosestAncestorProjHeuristic>();

    public (Dictionary<AbsolutePath, HashSet<AbsolutePath>>, HashSet<AbsolutePath> unableToFindProj) GetProjectToFileMapping(HashSet<AbsolutePath> filepaths, AbsolutePath? projFilter)
    {

        if(projFilter is not null)
        {
            Logger.LogInformation("Project filter limiting project to only {FilePath}", projFilter.Path);
        }

        var fileToProj = new List<(AbsolutePath Path, AbsolutePath projPath)>();
        var unableToFindProj = new HashSet<AbsolutePath>();
        foreach (var filepath in filepaths)
        {
            Logger.LogInformation("Finding .csproj file for {FilePath}", filepath.Path);
            try
            {
                var projPath = GetProjByClosestAncestorHeuristic(filepath);
                if (projFilter == null || projPath == projFilter)
                {
                    fileToProj.Add((filepath, projPath));
                }
                else
                {
                    Logger.LogWarning("Skipping due to the project filter");
                    unableToFindProj.Add(filepath);
                }
            }
            catch (UnableToFindProjectException)
            {
                Logger.LogError("Unable to find project for {Filepath}", filepath.Path);
                unableToFindProj.Add(filepath);
            }
        }

        var projToFiles = fileToProj
            .GroupBy(pair => pair.projPath)
            .ToDictionary(
                group => group.Key,
                group => group.Select(x => x.Path).ToHashSet()
            );
        return (projToFiles,unableToFindProj);
    }

    private static AbsolutePath GetProjByClosestAncestorHeuristic(AbsolutePath filepath)
    {
        var curDir = Directory.GetParent(filepath.Path);
        const int maxNumberOfLoops = 100;
        foreach (var _ in Enumerable.Range(0, maxNumberOfLoops))
        {
            var filesInDir = Directory.GetFiles(curDir!.FullName);
            var csProjFilesInDir = filesInDir.Where(f => f.EndsWith(".csproj")).ToList();
            switch (csProjFilesInDir.Count)
            {
                case 0:
                    curDir = curDir.Parent ??
                             throw new UnableToFindProjectException($"No csproj file found in ancestors of {filepath}");
                    continue;
                case 1:
                    var projFile = csProjFilesInDir.Single();
                    Logger.LogInformation("Project {ProjFile} is closest ancestor", projFile);
                    return new AbsolutePath(projFile);
                default:
                    throw new UnableToFindProjectException(
                        $"{filepath} has multiple most recent ancestors {string.Join(", ", csProjFilesInDir)}");
            }
        }
        throw new UnableToFindProjectException($"Exceeded max depth of ancestor search for {filepath}");
    }

}