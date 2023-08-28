using Semtex.Models;

namespace Semtex;

public class CantFindDocumentException : Exception
{
    public readonly AbsolutePath Path;

    public CantFindDocumentException(AbsolutePath path)
    {
        Path = path;
    }
}