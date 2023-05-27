using System;

namespace Semtex.UT.NotSemanticallyEquivalent.Using;

public class Right
{
    public void CleanedUp()
    {
        var x = new ExternalObj();
        Console.WriteLine(x.Value);
        Console.WriteLine("after");
    }
}