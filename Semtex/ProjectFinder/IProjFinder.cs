namespace Semtex.ProjectFinder;

public interface IProjFinder
{
    public (Dictionary<string, HashSet<string>>, HashSet<string> unableToFindProj) GetProjectToFileMapping(HashSet<string> filepaths, string? projFilter);

}