namespace Semtex.Models;

public class CommitModel
{
    public CommitModel(string commitHash, List<FileModel> fileModels, long elapsedMilliseconds,DiffConfig diffConfig )
    {
        CommitHash = commitHash;
        FileModels = fileModels;
        ElapsedMilliseconds = elapsedMilliseconds;
        DiffConfig = diffConfig;
    }
    
    public required string CommitHash { get; init; }
    public bool SemanticallyEquivalent => FileModels.All(f => f.Status == Status.SemanticallyEquivalent);
    public List<FileModel> FileModels { get; }
    public long ElapsedMilliseconds { get; }
    public DiffConfig DiffConfig { get; }
}