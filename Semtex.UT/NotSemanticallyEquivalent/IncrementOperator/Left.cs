using System;

namespace Semtex.UT.NotSemanticallyEquivalent.IncrementOperator;

public class Left
{
    public int Increment(int i)
    {
        Console.WriteLine(i++);
        return i;
    }
}