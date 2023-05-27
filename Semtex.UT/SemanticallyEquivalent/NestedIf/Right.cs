using System;

namespace Semtex.UT.ShouldPass.NestedIf;

public class Right
{
    public static void DoSomething(bool flag1, bool flag2)
    {
        if (flag1 && flag2)
        {
            Console.WriteLine("Both True");
        }
    }
}