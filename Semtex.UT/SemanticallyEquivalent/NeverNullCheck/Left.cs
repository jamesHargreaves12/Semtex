namespace Semtex.UT.ShouldPass.NeverNullCheck;

public class Left
{
    public static int One()
    {
        int x = 1;
        if (x == null)
        {
            x += 1;
        }

        return x;
    }

}