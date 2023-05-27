using System;

namespace Semtex.UT.NotEquivalent.AddSideEffect;

public class Right
{
    public static int Double(int a)
    {
        Console.WriteLine($"Doubling {a}");
        return a * 2;
    }

}