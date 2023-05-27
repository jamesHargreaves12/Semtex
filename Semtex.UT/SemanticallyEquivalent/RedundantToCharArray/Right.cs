using System;

namespace Semtex.UT.ShouldPass.RedundantToCharArray;

public class Right
{
    public static void LogChars(string s)
    {
        foreach (var c in s)
        {
            Console.WriteLine(c);
        }
    }
}