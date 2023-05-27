using System.Collections.Generic;

namespace Semtex.UT.ShouldPass.CountVsAny;

public class Right
{
    public string HasStuff(List<object> flags)
    {
        if (flags.Count > 0)
        {
            return "Yes";
        }

        return "No";
    }

}