using System;
using System.Diagnostics;

namespace Semtex.UT.ShouldPass.AttributesSeperated;

public class Left
{
    [Obsolete]
    [Conditional("DEBUG")]
    public void OldMethod()
    {
    }
}