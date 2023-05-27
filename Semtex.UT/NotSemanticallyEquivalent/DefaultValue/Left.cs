using System;

namespace Semtex.UT.NotSemanticallyEquivalent.DefaultValue;

public class Left
{
    public void Log(int x = 1)
    {
        Console.WriteLine(x);
    }
}