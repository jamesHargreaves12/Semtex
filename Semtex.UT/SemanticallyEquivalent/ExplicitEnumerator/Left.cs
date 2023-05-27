using System.Collections.Generic;

namespace Semtex.UT.ShouldPass.ExplicitEnumerator;

public class Left
{
    public static IEnumerable<int> Iterate(List<int> items)
    {
        using (var en = items.GetEnumerator())
        {
            while (en.MoveNext())
            {
                yield return en.Current;
            }
        }

    }
}