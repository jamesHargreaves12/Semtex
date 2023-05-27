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

}