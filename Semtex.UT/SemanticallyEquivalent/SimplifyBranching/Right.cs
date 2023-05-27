using System;

namespace Semtex.UT.ShouldPass.SimplifyBranching;

public class Right
{
    public static void CountUp(int x)
    {
        while (x <= 10)
        {
            Console.WriteLine(x);
            x += 1;
        }
    }

}