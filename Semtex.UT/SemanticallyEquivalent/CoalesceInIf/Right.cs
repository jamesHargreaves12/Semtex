using System;

namespace Semtex.UT.SemanticallyEquivalent.CoalesceInIf;

public class Right
{
    public static void M(bool? flag)
    {
        if (flag != true) return;
        
        Console.WriteLine("Some text");
    }

    public static void M2(bool? flag)
    {
        if (flag == false) return;
        
        Console.WriteLine("Some text");
    }
}
