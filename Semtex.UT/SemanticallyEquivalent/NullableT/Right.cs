namespace Semtex.UT.ShouldPass.NullableT;

public class Right
{
    public static int SomeValue()
    {
        int? x = null;
        return x ?? 0;
    }

}