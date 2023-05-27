using System;

namespace Semtex.UT.ShouldPass.SimplifyBranching;

public class Left
{
    public static void CountUp(int x)
    {
        while (true)
        {
            if (x > 10)
            {
                break;
            }
            
            Console.WriteLine(x);
            x += 1;
        }

    }
}