using System;

namespace Semtex.UT.ShouldPass.NullCheck;

public class Right
{
    public static void LogIfNull(string? s)
    {
        if (s is null)
        {
            Console.WriteLine("S is null");
        }
    }
}