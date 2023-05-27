using System;

namespace Semtex.UT.NotSemanticallyEquivalent.NullCheck;

public class Right
{
    public static int Unwrap(Wrapper? x)
    {
        if (x is null)
        {
            Console.WriteLine("Are you sure you meant to give me null?");
        }

        return 1;
    }
}