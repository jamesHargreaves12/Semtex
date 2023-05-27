using System;

namespace Semtex.UT.ShouldPass.UnnecessaryUnsafe;

public class Left
{
    public unsafe void SayNothing()
    {
        unsafe
        {
            Console.WriteLine("Nothing");
        }
    }
}