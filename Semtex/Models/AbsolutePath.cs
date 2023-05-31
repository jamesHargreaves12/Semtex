namespace Semtex.Models;

public record AbsolutePath
{
    public readonly string Path;

    public AbsolutePath(string path)
    {
        Path = System.IO.Path.GetFullPath(path);
    }
};
