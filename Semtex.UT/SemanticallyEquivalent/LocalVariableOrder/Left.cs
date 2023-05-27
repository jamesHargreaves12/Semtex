using System;

namespace Semtex.UT.SemanticallyEquivalent.LocalVariableOrder;

public class Left
{
    public static int M(int a, int b)
    {
        int z;
        var y = a+1;
        var x = b+1;
        var w = new Wrapper();
        var t = w + w;
        a = b = 1;
        x = y = 1;
        Console.WriteLine(a+b);
        z = 1;
        Console.WriteLine(z);
        return x + y;
        
    }
}