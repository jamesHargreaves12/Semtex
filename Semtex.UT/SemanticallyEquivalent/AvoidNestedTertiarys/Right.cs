using System;

namespace Semtex.UT.ShouldPass.AvoidNestedTertiarys;

public class Right
{
    public static string LazyWhichTrue(bool flag1, bool flag2)
    {
        string x;
        if (flag1)
        {
            x = "first true";
        }
        else if(flag2)
        {
            x = "second true";
        }
        else
        {
            x = "none true";
        }

        Console.WriteLine(x);
        return x;
    }
}