using System;

namespace Semtex.UT.NotSemanticallyEquivalent.Using;

public class Left
{
    public void CleanedUp()
    {
        using (var x = new ExternalObj())
        {
            Console.WriteLine(x.Value);  
        }
        Console.WriteLine("after");
    }
}