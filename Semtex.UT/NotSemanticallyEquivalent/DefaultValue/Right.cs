using System;

namespace Semtex.UT.NotSemanticallyEquivalent.DefaultValue;

public class Right
{
    public void Log(int x = 2)
    {
        Console.WriteLine(x);
    }
}