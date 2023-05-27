using System.Collections.Generic;
using System.Linq;

namespace Semtex.UT.SemanticallyEquivalent.RenameInFrom;

public class Left
{
    public static int[] EvenPlusOne(List<int> xs)
    {
        return (from x in xs where x % 2 == 0 select x + 1).ToArray();
    }
}