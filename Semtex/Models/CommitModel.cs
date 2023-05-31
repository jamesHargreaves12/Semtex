namespace Semtex.Models;

public class CommitModel
{
    public CommitModel(string commitHash, List<FileModel> fileModels, long elapsedMilliseconds)
    {
        CommitHash = commitHash;
        FileModels = fileModels;
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    
    public required string CommitHash { get; init; }
    public bool SemanticallyEquivalent => FileModels.All(f => f.Status == Status.SemanticallyEquivalent);
    public List<FileModel> FileModels { get; }
    public long ElapsedMilliseconds { get; }
}