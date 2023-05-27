using System;

namespace Semtex.UT.NotSemanticallyEquivalent.DifferentException;

public class Left
{
    public void Risky()
    {
        throw new Exception();
    }
}