namespace Semtex.UT.ShouldPass.LocalAsConst;

public class Right
{
    public static string GetSecret()
    {
        const string s = "secret";
        return s;
    }
}