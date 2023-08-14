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
    
    public int MinMod7(List<int> items)
    {
        return items.MinBy(x => x % 7);
    }
    
    public int AnyOdd(List<int> items)
    {
        if (items.Any(x => x % 2 != 0))
        {
            return 1;
        }

        return 7;
    }

}