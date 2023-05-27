using System;

namespace Semtex.UT.SemanticallyEquivalent.LocalVariableRename;

public class Left
{
    public static int Eight()
    {
        Console.WriteLine();
        var WriteLine = 1;
        var z = 2;
        z += 2;
        WriteLine += 2;
        WriteLine += 1;
        return WriteLine + z;
    }
}


