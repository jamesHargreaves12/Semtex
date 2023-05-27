using System;

namespace Semtex.UT.ShouldPass.DefaultParam;

public class Right
{
    public static void Log(string? arg = default)
    {
        Console.WriteLine(arg);
    }
}