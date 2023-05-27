using System;

namespace Semtex.UT.NotSemanticallyEquivalent.ReorderSideEffects;

public class Right
{
    public static void DoubleLog()
    {
        Console.WriteLine("2");
        Console.WriteLine("1");
    }
}