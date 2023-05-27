namespace Semtex.UT.NotSemanticallyEquivalent.ReorderWithOverloadedOperator;

public class Right
{
    public static int AddSpecial(Wrapper l, Wrapper r)
    {
        var second = r + l;
        var first = l + r;
        return first.X + second.X;
    }
}