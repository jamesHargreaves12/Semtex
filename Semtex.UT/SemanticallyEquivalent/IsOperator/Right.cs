using System;
using Semtex.UT;

namespace Semtex.UT.ShouldPass.IsOperator;

public class Right
{
    public static string IsString(object s)
    {
        if (s is string)
        {
            return "True";
        }

        return "False";
    }
    
    public static void UnneededCast(object x)
    {
        if (x is Wrapper wrapper && wrapper.X>0)
        {
            Console.WriteLine("Something");
        }
    }
    
    public static void NoNeedForAs(object x)
    {
        if (!(x is Wrapper wrap))
        {
            return;
        }
        Console.WriteLine(wrap.X);
    }
    
    public static void IsInvert(int x)
    {
        if (x is not 1)
        {
            return; 
        }
        Console.WriteLine("Its 1!");
    }


}