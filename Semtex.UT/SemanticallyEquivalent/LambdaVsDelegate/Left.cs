using System.Collections.Generic;
using System.Linq;

namespace Semtex.UT.ShouldPass.LambdaVsDelegate;

public class Left
{
    public static IEnumerable<int> Double(List<int> items)
    {
        return items.Select(delegate(int i) { return i * 2; });
    }
}