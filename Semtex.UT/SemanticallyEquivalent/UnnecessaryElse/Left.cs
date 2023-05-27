namespace Semtex.UT.ShouldPass.UnnecessaryElse;

public class Left
{
    public static int GiveMePrev(int x)
    {
        var z = 1;
        if (x == 0)
        {
            var y = 0;
            return 0;
        }
        else
        {
            var y = 0;
            return x - 1;
        }
    }
}