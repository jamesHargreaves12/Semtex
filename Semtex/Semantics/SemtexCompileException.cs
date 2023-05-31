using Semtex.Models;

namespace Semtex.Semantics;

public class SemtexCompileException : Exception
{
    public AbsolutePath ProjectPath { get; }

    public SemtexCompileException(AbsolutePath projectPath, string message) : base(message)
    {
        ProjectPath = projectPath;
    }
}