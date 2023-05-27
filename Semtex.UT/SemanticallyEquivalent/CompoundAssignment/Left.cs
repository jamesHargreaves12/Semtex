namespace Semtex.UT.ShouldPass.CompoundAssignment;

public class Left
{
    public static int DoSomething()
    {
        var x = 2;
        x = x + 2;
        return x;
    }
}