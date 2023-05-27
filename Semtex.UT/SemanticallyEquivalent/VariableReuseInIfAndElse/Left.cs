using System;

namespace Semtex.UT.SemanticallyEquivalent.VariableReuseInIfAndElse;

public class Left
{
    // Catching a bug with RCS1211
    // Should come up with a better way of doing this.

    public static BasicOptions DoSomething(bool flag)
    {
        if (flag)
        {
            var x = BasicOptions.A;
            Console.WriteLine(x);
            return x;
        }
        else
        {
            var x = BasicOptions.B;
            Console.WriteLine($"Else {x}");
            return x;
        }
    }
}