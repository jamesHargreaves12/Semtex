using System.Collections.Generic;
using System.Linq;

namespace Semtex.UT.ShouldPass.ReorderUsings;

public class Left
{
    public static List<int> ToList( int[] vals)
    {
        return vals.ToList();
    }
}