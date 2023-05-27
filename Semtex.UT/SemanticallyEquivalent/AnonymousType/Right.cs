using System;

namespace Semtex.UT.SemanticallyEquivalent.AnonymousType;

public class Right
{
    public void M()
    {
        var x= new { double.MaxValue };
        Console.WriteLine(x);
    }
}