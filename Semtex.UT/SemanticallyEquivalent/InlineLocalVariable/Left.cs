using System;
using System.Collections.Generic;

namespace Semtex.UT.ShouldPass.InlineLocalVariable;

public class Left
{
    public static void PrintVals(Func<IEnumerable<string>> getter)
    {
        var items = getter();
        foreach (var i in items)
        {
            Console.WriteLine(i);
        }
    }
}