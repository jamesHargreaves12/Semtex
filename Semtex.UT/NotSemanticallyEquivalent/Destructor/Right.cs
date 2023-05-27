using System;

namespace Semtex.UT.NotSemanticallyEquivalent.Destructor;

public class Right
{
    ~Right()
    {
        Console.WriteLine("Destructing");
    }
}