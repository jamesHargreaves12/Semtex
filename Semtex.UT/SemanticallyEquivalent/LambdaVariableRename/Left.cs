using System.Collections.Generic;
using System.Linq;

namespace Semtex.UT.SemanticallyEquivalent.LambdaVariableRename;

public class Left
{
    public static int SumEvens(List<int> vals)
    {
        return vals.Where(x => x % 2 == 0).Sum();
    }
}