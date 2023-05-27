using System;
using System.Collections.Generic;

namespace Semtex.UT.SemanticallyEquivalent.ListPattern;

public class Left
{
    public static void M(List<string> xs)
    {
        switch (xs)
        {
            case [var x, ..]:
                Console.WriteLine("Head");
                break;
            default:
                Console.WriteLine("No Head");
                break;
        }
    }

}