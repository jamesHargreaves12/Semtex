namespace Semtex.UT.SemanticallyEquivalent.PatternMatch;

public class Left
{
    public object M(object? val)
    {
        if (val is int)
        {
            return (int)val;
        }

        if (val is long)
        {
            return (long)val;
        }

        return new();
    }
}