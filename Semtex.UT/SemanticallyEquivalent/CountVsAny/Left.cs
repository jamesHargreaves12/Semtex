using System.Collections.Generic;
using System.Linq;

namespace Semtex.UT.ShouldPass.CountVsAny;

public class Left
{
    public string HasStuff(List<object> flags)
    {
        if (flags.Any())
        {
            return "Yes";
        }

        return "No";
    }
}