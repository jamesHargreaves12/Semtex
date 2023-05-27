using System;

namespace Semtex.UT.NotSemanticallyEquivalent.UsedPrivate;

public class Right
{
    public int GetSomethingHidden()
    {
        return SomethingHidden();
    }

    private int SomethingHidden()
    {
        return 2;
    }
}