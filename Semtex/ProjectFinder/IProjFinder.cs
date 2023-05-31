using Semtex.Models;

namespace Semtex.ProjectFinder;

public interface IProjFinder
{
    public (Dictionary<AbsolutePath, HashSet<AbsolutePath>>, HashSet<AbsolutePath> unableToFindProj) GetProjectToFileMapping(HashSet<AbsolutePath> filepaths, AbsolutePath? projFilter);

}