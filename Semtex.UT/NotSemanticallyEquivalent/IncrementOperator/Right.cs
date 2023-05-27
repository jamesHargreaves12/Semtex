using System;

namespace Semtex.UT.NotSemanticallyEquivalent.IncrementOperator;

public class Right
{
    public int Increment(int i)
    {
        Console.WriteLine(++i);
        return i;
    }
}