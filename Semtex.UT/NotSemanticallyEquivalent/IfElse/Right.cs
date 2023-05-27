using System;

namespace Semtex.UT.NotSemanticallyEquivalent.IfElse;

public class Right
{
    public void TellMeSomething(bool flag)
    {
        if (flag)
        {
            Console.WriteLine("Something false");
        }
        else
        {
            Console.WriteLine("Something true");
        }
    }
}