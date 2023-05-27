using System.Text.Json;

namespace Semtex.ProjectFinder;

public class ExplicitFileMapToProj: IProjFinder
{
    private readonly string _rootFolder;
    private readonly Dictionary<string, List<string>> _fileMap;

    public ExplicitFileMapToProj(string fileMapPath, string rootFolder)
    {
        _rootFolder = rootFolder;
        _fileMap = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(fileMapPath))!;
    }

    public (Dictionary<string, HashSet<string>>, HashSet<string> unableToFindProj) GetProjectToFileMapping(HashSet<string> filepaths, string? projFilter)
    {
        var result = new Dictionary<string, HashSet<string>>();
        var unableToFindProj = new HashSet<string>();
        var relativeProjFilter = projFilter?.Replace(_rootFolder + "/", "");
        foreach (var absoluteFilepath in filepaths)
        {
            var relativeFilepath = absoluteFilepath.Replace(_rootFolder +"/", "");
            if (!_fileMap.ContainsKey(relativeFilepath))
            {
                unableToFindProj.Add(relativeFilepath);
                continue;
            }

            var projPaths = _fileMap[relativeFilepath]
                .Where(p => relativeProjFilter == null || p == relativeProjFilter)
                .ToList();
            if (!projPaths.Any())
            {
                unableToFindProj.Add(relativeFilepath);
                continue;
            }
            foreach (var projFp in projPaths)
            {
                var fullProjPath = Path.Join(_rootFolder, projFp);
                if (result.ContainsKey(fullProjPath))
                {
                    result[fullProjPath].Add(absoluteFilepath);
                }

                result[fullProjPath] = new HashSet<string>() { absoluteFilepath };
            }
        }

        return (result, unableToFindProj);
    }
}