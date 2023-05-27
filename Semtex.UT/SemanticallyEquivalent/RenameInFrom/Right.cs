using System.Collections.Generic;
using System.Linq;

namespace Semtex.UT.SemanticallyEquivalent.RenameInFrom;

public class Right
{
    public static int[] EvenPlusOne(List<int> xs)
    {
        return (from y in xs where y % 2 == 0 select y + 1).ToArray();
    }
}