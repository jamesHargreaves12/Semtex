using System;
using System.Diagnostics;

namespace Semtex.UT.ShouldPass.AttributesSeperated;

public class Right
{
    [Obsolete, Conditional("DEBUG")]
    public void OldMethod()
    {
    }
}