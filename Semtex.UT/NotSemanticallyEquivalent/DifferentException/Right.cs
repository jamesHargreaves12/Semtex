using System;

namespace Semtex.UT.NotSemanticallyEquivalent.DifferentException;

public class Right
{
    public void Risky()
    {
        throw new SystemException();
    }
}