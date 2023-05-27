using System.Collections.Generic;
using System.Linq;

namespace Semtex.UT.NotSemanticallyEquivalent.Linq;

public class Left
{
    public List<int> Print(List<int> items)
    {
        return items
            .Select(i => i * 2)
            .ToList();
    }
}