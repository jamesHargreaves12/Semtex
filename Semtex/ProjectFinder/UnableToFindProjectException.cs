namespace Semtex.ProjectFinder;

public class UnableToFindProjectException : Exception
{
    public UnableToFindProjectException(string s) : base(s)
    {
    }

    public UnableToFindProjectException()
    {
    }

    public UnableToFindProjectException(string message, Exception innerException) : base(message, innerException)
    {
    }
}