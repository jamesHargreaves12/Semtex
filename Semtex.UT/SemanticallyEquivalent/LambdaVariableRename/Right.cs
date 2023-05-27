using System.Collections.Generic;
using System.Linq;

namespace Semtex.UT.SemanticallyEquivalent.LambdaVariableRename;

public class Right
{
    public static int SumEvens(List<int> vals)
    {
        return vals.Where(y => y % 2 == 0).Sum();
    }
}