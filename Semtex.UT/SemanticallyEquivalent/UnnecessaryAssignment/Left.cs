namespace Semtex.UT.ShouldPass.UnnecessaryAssignment;

public class Left
{
    public static int DoubleSome(int x)
    {
        var y = 3;
        if (x == 1)
        {
            y = 2;
        }
        else
        {
            y = 4;
        }

        return y;
    }
}