using System;

namespace Semtex.UT.ShouldPass.RedundantStatement;

public class Left
{
    public static bool Answer(bool flag)
    {
        if (flag)
        {
            Console.WriteLine("True");
            return true;
        }

        return true;
    }
}