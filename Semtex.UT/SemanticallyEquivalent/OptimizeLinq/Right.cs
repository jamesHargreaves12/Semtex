using System.Collections.Generic;
using System.Linq;

namespace Semtex.UT.ShouldPass.OptimizeLinq;

public class Right
{
    public bool HasEven(List<int> items)
    {
        return items.Any(x => x % 2 == 0);
    }
    
    public IEnumerable<int> EvenAndThree (List<int> items)
    {
        return items.Where(x => x % 2 == 0 && x % 3 == 0);
    }
}