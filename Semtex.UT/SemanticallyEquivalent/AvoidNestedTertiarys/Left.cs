using System;

namespace Semtex.UT.ShouldPass.AvoidNestedTertiarys;

public class Left
{
    public static string LazyWhichTrue(bool flag1, bool flag2)
    {
        var x =  flag1 ? "first true" : flag2 ? "second true" : "none true";
        Console.WriteLine(x);
        return x;
    }
}