namespace Semtex.UT.NotSemanticallyEquivalent.NullCoalesce;

public class Right
{
    public static Wrapper? GetValue(Wrapper? w)
    {
        return w ?? new Wrapper(1);
    }
}