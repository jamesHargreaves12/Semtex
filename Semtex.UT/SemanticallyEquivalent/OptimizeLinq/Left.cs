using System.Collections.Generic;
using System.Linq;

namespace Semtex.UT.ShouldPass.OptimizeLinq;

public class Left
{
    public bool HasEven(List<int> items)
    {
        return items.Where(x => x % 2 == 0).Any();
    }
    
    public IEnumerable<int> EvenAndThree(List<int> items)
    {
        return items.Where(x => x % 2 == 0).Where(x => x % 3 == 0);
    }

    public int MinMod7(List<int> items)
    {
        return items.OrderBy(x => x % 7).FirstOrDefault();
    }

    public int AnyOdd(List<int> items)
    {
        if (!items.All(x => x % 2 == 0))
        {
            return 1;
        }

        return 7;
    }

}