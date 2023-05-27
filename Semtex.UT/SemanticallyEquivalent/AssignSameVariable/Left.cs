using System;

namespace Semtex.UT.ShouldPass.AssignSameVariable;

public class Left
{
    public int One(bool x)
    {
        x = x;
        if (x)
        {
            Console.WriteLine("it does");
        }

        return 1;
    }

}