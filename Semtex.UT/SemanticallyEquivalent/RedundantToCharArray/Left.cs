using System;

namespace Semtex.UT.ShouldPass.RedundantToCharArray;

public class Left
{
    public static void LogChars(string s)
    {
        foreach (var c in s.ToCharArray())
        {
            Console.WriteLine(c);
        }
    }
}