namespace Semtex.UT.ShouldPass.RedundantCast;

public class Left
{
    public static int UnWrap(Wrapper wrapper)
    {
        return ((Wrapper)wrapper).X;
    }
}