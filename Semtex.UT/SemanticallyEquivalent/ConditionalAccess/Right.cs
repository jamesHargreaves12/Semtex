namespace Semtex.UT.ShouldPass.ConditionalAccess;

public class Right
{
    public static Wrapper? ReWrap(Wrapper? x)
    {
        return x?.GetClone();
    }
}