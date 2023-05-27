using System;

namespace Semtex.UT.SemanticallyEquivalent.LocalVariableOrder;

public class Right
{
    public static int M(int a, int b)
    {
        var x = b+1;
        var y = a+1;
        int z;
        var w = new Wrapper();
        var t = w + w;
        x = y = 1;
        a = b = 1;
        Console.WriteLine(a+b);
        z = 1;
        Console.WriteLine(z);
        return x + y;
    }
}