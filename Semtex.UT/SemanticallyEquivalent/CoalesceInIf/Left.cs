using System;

namespace Semtex.UT.SemanticallyEquivalent.CoalesceInIf;

public class Left
{
    public static void M(bool? flag)
    {
        if ((flag ?? false))
        {
            Console.WriteLine("Some text");
        }
    }
    
    public static void M2(bool? flag)
    {
        if ((flag ?? true))
        {
            Console.WriteLine("Some text");
        }
    }

}