namespace Semtex.UT.ShouldPass.IfToExpression;

public class Right
{
    public static bool Truthy(int val)
    {
        bool f;
        f = val != 0;

        return f;
    }
}