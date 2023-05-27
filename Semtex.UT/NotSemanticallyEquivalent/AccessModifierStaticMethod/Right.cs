using System;

namespace Semtex.UT.NotSemanticallyEquivalent.AccessModifierStaticMethod;

public class Right
{
    protected static void Log()
    {
        Console.WriteLine("something");
    }
}