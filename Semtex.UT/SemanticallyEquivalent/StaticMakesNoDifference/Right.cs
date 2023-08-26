using System;

namespace Semtex.UT.SemanticallyEquivalent.StaticMakesNoDifference;

public class Right: Wrapper
{
    public static int F = 1;
    public int F2 = 2;
    public int X = 2;

    public void M(Wrapper w)
    {
        X = 3;
        CanBeStatic(w);
    }

    private static void CanBeStatic(Wrapper w)
    {
        Console.WriteLine($"Something {F} {w.X}");
    }
    private void CannotBeStatic(Wrapper w)
    {
        Console.WriteLine(base.X);
    }
}