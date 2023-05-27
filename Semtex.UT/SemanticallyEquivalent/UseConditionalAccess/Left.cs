using System;

namespace Semtex.UT.ShouldPass.UseConditionalAccess;

public class Left
{
    public string DoubleA(string? s)
    {
        if (s != null && s.StartsWith("a"))
        {
            Console.WriteLine("Yep doubling");
            return "a" + s;
        }

        return s;
    } 
}