using System;

namespace Semtex.UT.SemanticallyEquivalent.StaticMakesNoDifference;

public class Left
{
    public static int F = 1;
    public int F2 = 2;
    public void M(Wrapper w)
    {
        CanBeStatic(w);
    }

    private void CanBeStatic(Wrapper w)
    {
        Console.WriteLine($"Something {F} {w.X}");
    }
}