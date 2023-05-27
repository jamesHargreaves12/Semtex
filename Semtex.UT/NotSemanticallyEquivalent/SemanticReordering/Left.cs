namespace Semtex.UT.NotSemanticallyEquivalent.SemanticReordering;

public class Left
{
    public static int DoublePlus3(int x, int y)
    {
        x = y + 1;
        y = x + 2;
        return x + y;
    }
}