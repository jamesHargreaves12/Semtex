namespace Semtex.UT.NotSemanticallyEquivalent.SemanticReordering;

public class Right
{
    public static int DoublePlus3(int x, int y)
    {
        y = x + 2;
        x = y + 1;
        return x + y;
    }
}