using System;

namespace Semtex.UT.ShouldPass.RedundantCast;

public class Left
{
    public static int UnWrap(Wrapper wrapper)
    {
        return ((Wrapper)wrapper).X;
    }

    public static void Main(int x)
    {
        Console.Write((int)x);
    }
}