using System;

namespace Semtex.UT.SemanticallyEquivalent.LocalVariableRename;

public class Right
{
    public static int Eight()
    {
        Console.WriteLine();
        var y = 1;
        var w = 2;
        w = w + 2;
        y += 2;
        y += 1;
        return y + w;
    }
}