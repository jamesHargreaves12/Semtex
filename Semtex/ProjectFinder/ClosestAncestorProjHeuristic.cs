using Microsoft.Extensions.Logging;
using Semtex.Logging;

namespace Semtex.ProjectFinder;

// If we have other ways of deriving project mapping then then it probably makes sense to split this up
public sealed class ClosestAncestorProjHeuristic : IProjFinder
{
    private static readonly ILogger<ClosestAncestorProjHeuristic> Logger = SemtexLog.LoggerFactory.CreateLogger<ClosestAncestorProjHeuristic>();

    public (Dictionary<string, HashSet<string>>, HashSet<string> unableToFindProj) GetProjectToFileMapping(HashSet<string> filepaths, string? projFilter)
    {
        var fileToProj = new List<(string filename, string projName)>();
        var unableToFindProj = new HashSet<string>();
        foreach (var filepath in filepaths)
        {
            Logger.LogInformation("Finding .csproj file for {FilePath}", filepath);
            try
            {
                var projName = GetProjByClosestAncestorHeuristic(filepath);
                if (projFilter == null || projName == projFilter)
                {
                    fileToProj.Add((filepath, projName));
                }
                else
                {
                    Logger.LogWarning("Skipping {Filepath} due to the project filter", filepath);
                    unableToFindProj.Add(filepath);
                }
            }
            catch (UnableToFindProjectException)
            {
                Logger.LogError("Unable to find project for {Filepath}", filepath);
                unableToFindProj.Add(filepath);
            }
        }

        var projToFiles = fileToProj
            .GroupBy(pair => pair.projName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(x => x.filename).ToHashSet()
            );
        return (projToFiles,unableToFindProj);
    }

    private static string GetProjByClosestAncestorHeuristic(string filepath)
    {
        var curDir = Directory.GetParent(filepath);
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
                    Logger.LogInformation("Project {ProjFile} is closest ancestor of {Filepath}", projFile, filepath);
                    return projFile;
                default:
                    throw new UnableToFindProjectException(
                        $"{filepath} has multiple most recent ancestors {string.Join(", ", csProjFilesInDir)}");
            }
        }
        throw new UnableToFindProjectException($"Exceeded max depth of ancestor search for {filepath}");
    }

}