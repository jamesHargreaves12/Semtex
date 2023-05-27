using System;

namespace Semtex.UT.NotSemanticallyEquivalent.ReorderSideEffects;

public class Left
{
    public static void DoubleLog()
    {
        Console.WriteLine("1");
        Console.WriteLine("2");
    }
}