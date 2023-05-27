using System;

namespace Semtex.UT.NotSemanticallyEquivalent.InvertingIf;

public class Right
{
    public void ConditionalPrint(bool flag)
    {
        if (!flag)
        {
            Console.WriteLine("Some value");
        }
    }
}