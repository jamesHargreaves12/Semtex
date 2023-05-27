
namespace Semtex.Models;

public class FileModel
{
    public FileModel(string filepath, Status status)
    {
        Filepath = filepath;
        Status = status;
    }

    public string Filepath { get; }
    public Status Status { get; }
}