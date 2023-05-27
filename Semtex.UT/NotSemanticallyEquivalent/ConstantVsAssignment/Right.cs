using System;

namespace Semtex.UT.NotSemanticallyEquivalent.ConstantVsAssignment;

public class Right
{
    public static int Log()
    {
        var x = 1;
        x = 3;
        Console.WriteLine(2);
        return x;
    }

}