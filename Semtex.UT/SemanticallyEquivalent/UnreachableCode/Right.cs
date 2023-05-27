namespace Semtex.UT.ShouldPass.UnreachableCode;

public class Right
{
    public static int Double(int x)
    {
        if (true)
        {
            return x*2;
        }
    }
}