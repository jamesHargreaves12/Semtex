namespace Semtex.UT.ShouldPass.SimplifyConditional;

public class Left
{
    public static bool Identity(bool flag)
    {
        var x = flag ? true : false;
        return x;
    }
}