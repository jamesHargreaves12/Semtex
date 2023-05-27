using System.Collections.Generic;
using System.Linq;

namespace Semtex.UT.ShouldPass.LambdaVsDelegate;

public class Right
{
    public static IEnumerable<int> Double(List<int> items)
    {
        return items.Select((int i) => i * 2);
    }

}