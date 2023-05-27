namespace Semtex.UT.ShouldPass.UnnecessaryAssignment;

public class Right
{
    public static int DoubleSome(int x)
    {
        var y = 3;
        if (x == 1)
        {
            return 2;
        }
        else
        {
            return 4;
        }
    }

}