namespace Semtex.UT.ShouldPass.ConstantOverField;

public class Left
{
    private static readonly int _one = 1;
    public int GetOne()
    {
        return _one;
    }
}