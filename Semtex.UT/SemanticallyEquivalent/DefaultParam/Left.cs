using System;

namespace Semtex.UT.ShouldPass.DefaultParam;

public class Left
{
    public static void Log(string? arg = default(string))
    {
        Console.WriteLine(arg);
    }
}