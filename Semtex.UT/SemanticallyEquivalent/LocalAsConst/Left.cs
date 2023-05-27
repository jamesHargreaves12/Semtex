namespace Semtex.UT.ShouldPass.LocalAsConst;

public class Left
{
    public static string GetSecret()
    {
        var s = "secret";
        return s;
    }
}