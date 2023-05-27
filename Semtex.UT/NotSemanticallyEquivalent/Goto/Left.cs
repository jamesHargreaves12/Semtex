using System;

namespace Semtex.UT.NotSemanticallyEquivalent.Goto;

public class Left
{
    public int Trick(bool flag)
    {
        if (flag)
        {
            goto trick;
        }

        if (true)
        {
            return 1;
        }
        
        trick:
        Console.WriteLine("You found it");
        return 2;
    }
}