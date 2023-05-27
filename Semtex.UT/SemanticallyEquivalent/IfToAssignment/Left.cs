namespace Semtex.UT.ShouldPass.IfToExpression;

public class Left
{
    public static bool Truthy(int val)
    {
        bool f;
        if (val != 0)
        {
            f = true;
        }
        else
        {
            f = false;
        }

        return f;
    }
}