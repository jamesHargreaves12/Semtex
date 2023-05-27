using System;

namespace Semtex.UT.NotSemanticallyEquivalent.NewException;

public class Right
{
    public void Print(int x)
    {
        if (x == 0)
        {
            throw new ArgumentException("I don't like 0s", nameof(x));
        }

        Console.WriteLine(x);
    }
}