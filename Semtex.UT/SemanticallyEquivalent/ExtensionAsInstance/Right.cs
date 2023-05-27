using System.Collections.Generic;
using System.Linq;

namespace Semtex.UT.ShouldPass.ExtensionAsInstance;

public class Right
{
    public static List<string> GiveMeStrings(List<int> vals)
    {
        return vals.Select(i => i.ToString()).ToList();
    }
}