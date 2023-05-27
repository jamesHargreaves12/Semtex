namespace Semtex.UT.ShouldPass.UnnecessaryElse;

public class Right
{
    public static int GiveMePrev(int x)
    {
        if (x == 0)
        {
            return 0;
        }
        return x - 1;
    }
}