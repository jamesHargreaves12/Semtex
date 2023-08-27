namespace Semtex;

public class CantFindDocumentException : Exception
{
    public CantFindDocumentException()
    {
    }
    
    public CantFindDocumentException(string message) : base(message)
    {
    }


    public CantFindDocumentException(string message, Exception innerException) : base(message, innerException)
    {
    }
}