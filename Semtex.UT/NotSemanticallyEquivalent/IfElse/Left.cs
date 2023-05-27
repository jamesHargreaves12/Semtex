using System;

namespace Semtex.UT.NotSemanticallyEquivalent.IfElse;

public class Left
{
    public void TellMeSomething(bool flag)
    {
        if (flag)
        {
            Console.WriteLine("Something true");
        }
        else
        {
            Console.WriteLine("Something false");
        }
    }
}