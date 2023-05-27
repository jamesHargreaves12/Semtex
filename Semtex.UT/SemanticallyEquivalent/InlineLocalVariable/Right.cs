using System;
using System.Collections.Generic;

namespace Semtex.UT.ShouldPass.InlineLocalVariable;

public class Right
{
    public static void PrintVals(Func<IEnumerable<string>> getter)
    {
        foreach (var i in getter())
        {
            Console.WriteLine(i);
        }
    }
}