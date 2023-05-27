namespace Semtex.UT.NotSemanticallyEquivalent.ReorderWithOverloadedOperator;

public class Left
{
    public static int AddSpecial(Wrapper l, Wrapper r)
    {
        var first = l + r;
        var second = r + l;
        return first.X + second.X;
    }
}