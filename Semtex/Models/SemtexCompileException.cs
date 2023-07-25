namespace Semtex.Models;

public class SemtexCompileException : Exception
{
    public AbsolutePath ProjectPath { get; }

    public SemtexCompileException(AbsolutePath projectPath, string message) : base(message)
    {
        ProjectPath = projectPath;
    }
}