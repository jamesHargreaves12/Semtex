using System;

namespace Semtex.UT.ShouldPass.UnneededCase;

public class Right
{
    public static void DoSomething(int val)
    {
        switch (val)
        {
            case 0:
                Console.WriteLine("Small");
                break;
            default:
                Console.WriteLine("Big");
                break;
        }
    }
}