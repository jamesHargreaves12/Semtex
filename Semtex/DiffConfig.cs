using Semtex.Models;

namespace Semtex;

public record DiffConfig
{
    public DiffConfig(HashSet<AbsolutePath> addedFilepaths, HashSet<AbsolutePath> removedFilepaths,
        HashSet<(AbsolutePath Source, AbsolutePath Target, int Similarity)> renamedFilepaths,
        List<AbsolutePath> allSourceFilePaths, HashSet<AbsolutePath> sourceCsFilepaths,
        HashSet<AbsolutePath> targetCsFilepaths, string targetSha, string sourceSha)
    {
        AddedFilepaths = addedFilepaths;
        RemovedFilepaths = removedFilepaths;
        RenamedFilepaths = renamedFilepaths;
        AllSourceFilePaths = allSourceFilePaths;
        SourceCsFilepaths = sourceCsFilepaths;
        TargetCsFilepaths = targetCsFilepaths;
        TargetSha = targetSha;
        SourceSha = sourceSha;
    }

    internal HashSet<AbsolutePath> AddedFilepaths { get; }
    internal HashSet<AbsolutePath> RemovedFilepaths { get; }
    internal HashSet<(AbsolutePath Source, AbsolutePath Target, int Similarity)> RenamedFilepaths { get; }
    internal List<AbsolutePath> AllSourceFilePaths { get; }
    internal HashSet<AbsolutePath> SourceCsFilepaths { get; }
    internal HashSet<AbsolutePath> TargetCsFilepaths { get; }
    public string TargetSha { get; }
    public string SourceSha { get; }

    internal AbsolutePath GetTargetFilepath(AbsolutePath sourceFilepath)
    {
        if (RenamedFilepaths.Any(x => x.Source == sourceFilepath))
        {
            return RenamedFilepaths.First(x => x.Source == sourceFilepath).Target;
        }

        return sourceFilepath;
    }

}
