using System;

namespace Semtex.UT.ShouldPass.ExceptionConstructors;

public class Right: Exception
{
    public Right(string message) : base(message)
    {
    }

    public Right()
    {
    }

    public Right(string message, Exception innerException) : base(message, innerException)
    {
    }
}