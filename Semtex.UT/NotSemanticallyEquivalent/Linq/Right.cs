using System.Collections.Generic;
using System.Linq;

namespace Semtex.UT.NotSemanticallyEquivalent.Linq;

public class Right
{
    public List<int> Print(List<int> items)
    {
        return items
            .Where(i => i % 2 == 0)
            .Select(i => i * 2)
            .ToList();
    }
}