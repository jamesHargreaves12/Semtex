using System;

namespace Semtex.UT.NotSemanticallyEquivalent.Goto;

public class Right
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
        return 2;
    }

}