using System;

namespace Semtex.UT.ShouldPass.NestedIf;

public class Left
{
    public static void DoSomething(bool flag1, bool flag2)
    {
        if (flag1)
        {
            if (flag2)
            {
                Console.WriteLine("Both True");
            }
        }
    }
}