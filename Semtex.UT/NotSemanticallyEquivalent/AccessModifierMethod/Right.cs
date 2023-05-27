using System;

namespace Semtex.UT.NotSemanticallyEquivalent.AccessModifierMethod;

public class Right
{
    public void AccessableMethod()
    {
        UsedMethod();
    }

    private void UsedMethod()
    {
        Console.WriteLine("Value");
    }
}