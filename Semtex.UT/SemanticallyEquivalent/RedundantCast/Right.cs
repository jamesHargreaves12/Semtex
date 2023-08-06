using System;

namespace Semtex.UT.ShouldPass.RedundantCast;

public class Right
{
    public static int UnWrap(Wrapper wrapper)
    {
        return wrapper.X;
    }
    public static void Main(int x)
    {
        Console.Write(x);
    }

}