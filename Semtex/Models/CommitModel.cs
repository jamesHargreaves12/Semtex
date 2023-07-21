using System.Text.Json.Serialization;

namespace Semtex.Models;

public class CommitModel
{
    private static readonly List<Status> SafeStatuses = new() { Status.SemanticallyEquivalent, Status.SafeFile };

    public CommitModel(string commitHash, List<FileModel> fileModels, long elapsedMilliseconds,DiffConfig diffConfig )
    {
        CommitHash = commitHash;
        FileModels = fileModels;
        ElapsedMilliseconds = elapsedMilliseconds;
        DiffConfig = diffConfig;
    }
    
    public required string CommitHash { get; init; }
    public bool SemanticallyEquivalent => FileModels.All(f => SafeStatuses.Contains(f.Status));
    public List<FileModel> FileModels { get; }
    public long ElapsedMilliseconds { get; }
    
    [JsonIgnore]
    public DiffConfig DiffConfig { get; }
}