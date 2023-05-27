using System;
using System.Reflection;

namespace Semtex.UT.NotSemanticallyEquivalent.AccessModifierMethod;

public class Left
{
    public void AccessableMethod()
    {
        UsedMethod();
    }

    internal void UsedMethod()
    {
        Console.WriteLine("Value");
    }

}