using System;

namespace Semtex.UT.ShouldPass.UnnecessaryElse;

public class Right
{
    public static int GiveMePrev(int x)
    {
        if (x == 0)
        {
            return 0;
        }
        return x - 1;
    }
    
    
    public static int M(int x, bool flag)
    {
        switch (x)
        {
            case 1:
                if (flag)
                {
                    var y = x;
                    Console.WriteLine(y);
                }

                break;
            case 5:
                Console.WriteLine(2);
                break;
            case 2:
                if (flag)
                {
                    var z = x;
                    Console.WriteLine(z+3);
                    return 1;
                }
                else
                {
                    var y = x;
                    Console.WriteLine(y+2);
                }

                break;
        }

        return 2;
    }

}