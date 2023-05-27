using System.Collections.Generic;

namespace Semtex.UT.ShouldPass.ExplicitEnumerator;

public class Right
{
    public static IEnumerable<int> Iterate(List<int> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }

}