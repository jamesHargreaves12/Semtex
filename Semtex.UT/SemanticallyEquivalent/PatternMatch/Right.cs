namespace Semtex.UT.SemanticallyEquivalent.PatternMatch;

public class Right
{
    public object M(object? val)
    {
        if (val is int x)
        {
            return x;
        }

        if (val is long x3)
        {
            return x3;
        }

        return new object();
    }
}