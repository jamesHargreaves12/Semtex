using System;

namespace Semtex.UT.ShouldPass.NullableT;

public class Left
{
    public static int SomeValue()
    {
        Nullable<int> x = null;
        return x ?? 0;
    }

}