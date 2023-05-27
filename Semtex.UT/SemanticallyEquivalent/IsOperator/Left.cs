using System;

namespace Semtex.UT.ShouldPass.IsOperator;

public class Left
{
    public static string IsString(object s)
    {
        if (s as string != null)
        {
            return "True";
        }

        return "False";
    }

    public static void UnneededCast(object x)
    {
        if (x is Wrapper && ((Wrapper)x).X>0)
        {
            Console.WriteLine("Something");
        }
    }

    public static void IsInvert(int x)
    {
        if (x is 1)
        {
            Console.WriteLine("Its 1!");
        }
    }

    public static void NoNeedForAs(object x)
    {
        var wrap = x as Wrapper;
        if (wrap == null)
        {
            return;
        }
        Console.WriteLine(wrap.X);
    }
}