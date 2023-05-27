namespace Semtex.UT.ShouldPass.UnusedPrivateMethods;

public class Left
{
    private string UnusedField { get; set; }
    private static string UnusedStatic { get; set; }
    public string Secret { get; }

    public Left(string secret)
    {
        Secret = secret;
    }

    private static int StaticOne()
    {
        return 1;
    }
    private string NonStaticHi()
    {
        return "Hi";
    }
}