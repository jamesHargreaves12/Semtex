namespace Semtex.UT.ShouldPass.RedundantBraces;

public class Right
{
    public static string DoSomething(int x)
    {
        if ((x > 0))
        {
            return "a";
        }
        return "b";
    }
}