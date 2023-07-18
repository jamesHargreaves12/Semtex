namespace Semtex.Models;

public record AbsolutePath
{
    public readonly string Path;

    public AbsolutePath(string path)
    {
        // Perhaps this should be an opt in feature rather than the default behaviour.
        Path = System.IO.Path.GetFullPath(path);
    }

    public AbsolutePath Join(string relativePath)
    {
        return new AbsolutePath(System.IO.Path.Join(Path, relativePath));
    }

    public string GetRelativePath(string childPath)
    {
        if (!childPath.StartsWith(Path))
        {
            throw new ArgumentException($"{childPath} not rooted at {Path}");
        }

        return childPath.Replace(Path + "/", "");
    }

    public string GetRelativePath(AbsolutePath childPath)
    {
        return GetRelativePath(childPath.Path);
    }
}
