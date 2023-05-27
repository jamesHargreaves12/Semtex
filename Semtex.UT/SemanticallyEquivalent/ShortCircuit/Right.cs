using System;

namespace Semtex.UT.ShouldPass.ShortCircuit;

public class Right
{
    public static void LogIfBoth(bool flag1, bool flag2)
    {
        if (flag1 && flag2)
        {
            Console.WriteLine("Both");
        }
    }

}