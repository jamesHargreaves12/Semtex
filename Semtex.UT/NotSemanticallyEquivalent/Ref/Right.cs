using System;

namespace Semtex.UT.NotSemanticallyEquivalent.Ref;

public class Right
{
    public static int Method(ref int x)
    {
        x = 2;
        return x;
    }
}