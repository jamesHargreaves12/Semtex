using System.Text.Json;
using Semtex.Models;

namespace Semtex.ProjectFinder;

public class ExplicitFileMapToProj : IProjFinder
{
    private readonly AbsolutePath _rootFolder;
    private readonly Dictionary<string, List<string>> _fileMap;

    public ExplicitFileMapToProj(AbsolutePath fileMapPath, AbsolutePath rootFolder)
    {
        _rootFolder = rootFolder;
        _fileMap = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(fileMapPath.Path))!;
    }

    public (Dictionary<AbsolutePath, HashSet<AbsolutePath>>, HashSet<AbsolutePath> unableToFindProj) GetProjectToFileMapping(HashSet<AbsolutePath> filepaths, AbsolutePath? projFilter)
    {
        var result = new Dictionary<AbsolutePath, HashSet<AbsolutePath>>();
        var unableToFindProj = new HashSet<AbsolutePath>();
        var relativeProjFilter = projFilter is not null ? _rootFolder.GetRelativePath(projFilter) : null;
        foreach (var absoluteFilepath in filepaths)
        {
            var relativeFilepath = _rootFolder.GetRelativePath(absoluteFilepath);
            if (!_fileMap.ContainsKey(relativeFilepath))
            {
                unableToFindProj.Add(absoluteFilepath);
                continue;
            }

            var projPaths = _fileMap[relativeFilepath]
                .Where(p => relativeProjFilter == null || p == relativeProjFilter)
                .ToList();
            if (!projPaths.Any())
            {
                unableToFindProj.Add(absoluteFilepath);
                continue;
            }
            foreach (var projFp in projPaths)
            {
                var fullProjPath = _rootFolder.Join(projFp);
                if (result.ContainsKey(fullProjPath))
                {
                    result[fullProjPath].Add(absoluteFilepath);
                }

                result[fullProjPath] = new HashSet<AbsolutePath>() { absoluteFilepath };
            }
        }

        return (result, unableToFindProj);
    }
}