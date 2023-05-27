using System;
using System.Collections.Generic;
using System.Linq;

namespace Semtex.UT.ShouldPass.AnonomousFunctionVsMethodGroup;

public class Left
{
    public static List<T2> SelectList<T,T2>(IEnumerable<T> list, Func<T, T2> fn)
    {
        return list.Select(fn).ToList();
    }
}