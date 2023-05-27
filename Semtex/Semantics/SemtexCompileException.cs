namespace Semtex.Semantics;

public class SemtexCompileException : Exception
{
    public string ProjectPath { get; }

    public SemtexCompileException(string projectPath, string message) : base(message)
    {
        ProjectPath = projectPath;
    }
}