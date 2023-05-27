using System;

namespace Semtex.UT.ShouldPass.NullCheck;

public class Left
{
    public static void LogIfNull(string? s)
    {
        if (s is null)
        {
            Console.WriteLine("S is null");
        }
    }
}