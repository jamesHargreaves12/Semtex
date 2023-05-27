using System;

namespace Semtex.UT.NotSemanticallyEquivalent.ConstantVsAssignment;

public class Left
{
    public static int Log()
    {
        var x = 1;
        x = 3;
        Console.WriteLine(x=2);
        return x;
    }
}