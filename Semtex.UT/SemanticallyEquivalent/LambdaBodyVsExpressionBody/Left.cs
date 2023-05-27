using System.Collections.Generic;
using System.Linq;

namespace Semtex.UT.ShouldPass.LambdaBodyVsExpressionBody;

public class Left
{
    public static IEnumerable<int> Double(IEnumerable<int> items)
    {
        return items.Select(x => x * 2);
    }
}