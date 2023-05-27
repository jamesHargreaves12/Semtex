using System;

namespace Semtex.UT.ShouldPass.UseConditionalAccess;

public class Right
{
    public string DoubleA(string? s)
    {
        if (s?.StartsWith("a") == true)
        {
            Console.WriteLine("Yep doubling");
            return "a" + s;
        }

        return s;
    }
}