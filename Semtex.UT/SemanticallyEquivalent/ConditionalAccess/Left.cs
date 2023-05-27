namespace Semtex.UT.ShouldPass.ConditionalAccess;

public class Left
{
    public static Wrapper? ReWrap(Wrapper? x)
    {
        return x != null ? x.GetClone() : null;
    }
}