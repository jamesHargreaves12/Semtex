namespace Semtex.UT.ShouldPass.Caret;

public class Right
{
    public static bool Xor(bool l, bool r)
    {
        return (l && !r) || (!l && r);
    }
}