using System;

namespace Semtex.UT.NotSemanticallyEquivalent.InvertingIf;

public class Left
{
    public void ConditionalPrint(bool flag)
    {
        if (flag)
        {
            Console.WriteLine("Some value");
        }
    }
}