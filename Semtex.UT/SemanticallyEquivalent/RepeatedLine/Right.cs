using System;

namespace Semtex.UT.SemanticallyEquivalent.RepeatedLine;

public class Right
{
    public static void M()
    {
        var x = 1;
        x = x + 1;
        x = x + 1;
        Console.WriteLine(x);
    }
}
