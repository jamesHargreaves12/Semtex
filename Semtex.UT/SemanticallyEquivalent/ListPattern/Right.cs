using System;
using System.Collections.Generic;

namespace Semtex.UT.SemanticallyEquivalent.ListPattern;

public class Right
{
    public static void M(List<string> xs)
    {
        switch (xs)
        {
            // TODO why isn't this considered in SemanticModel.GetVariable Declerations?
            case [var x, ..]:
                Console.WriteLine("Head");
                break;
            default:
                Console.WriteLine("No Head");
                break;
        }
    }

}