using System;

namespace Semtex.UT.ShouldPass.UnneededCase;

public class Left
{
    public static void DoSomething(int val)
    {
        switch (val)
        {
            case 0:
                Console.WriteLine("Small");
                break;
            case 1:
            default:
                Console.WriteLine("Big");
                break;
        }
    }
}