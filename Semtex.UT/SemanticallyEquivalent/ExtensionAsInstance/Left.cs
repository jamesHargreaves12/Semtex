using System.Collections.Generic;
using System.Linq;

namespace Semtex.UT.ShouldPass.ExtensionAsInstance;

public class Left
{
    public static List<string> GiveMeStrings(List<int> vals)
    {
        return Enumerable.Select(vals, i => i.ToString()).ToList();
    }
}