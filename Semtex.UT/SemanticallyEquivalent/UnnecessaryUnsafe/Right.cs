using System;

namespace Semtex.UT.ShouldPass.UnnecessaryUnsafe;

public class Right
{
    public unsafe void SayNothing()
    {
        {
            Console.WriteLine("Nothing");
        }
    }
}