using System;

namespace Semtex.UT.ShouldPass.AlwaysFalse;

public class Left
{
    public static void ImpossibleLog(uint i)
    {
        if ( i >= 0)
        {
            return;
        }
        Console.WriteLine("Not possible");
    } 
}