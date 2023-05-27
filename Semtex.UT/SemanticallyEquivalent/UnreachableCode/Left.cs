namespace Semtex.UT.ShouldPass.UnreachableCode;

public class Left
{
    public static int Double(int x)
    {
        if (true)
        {
            return x*2;
        }

        return x;
        var y = 1;
        return y;
    }
}