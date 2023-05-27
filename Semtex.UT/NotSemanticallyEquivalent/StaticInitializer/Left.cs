using System;

namespace Semtex.UT.NotSemanticallyEquivalent.StaticInitializer;

public class Left
{
    static Left()
    {
        Console.WriteLine("Static Initing");
    }
}