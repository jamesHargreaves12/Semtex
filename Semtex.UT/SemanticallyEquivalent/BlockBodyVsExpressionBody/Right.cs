namespace Semtex.UT.ShouldPass.BlockBodyVsExpressionBody;

public class Right
{
    public static string StringGetter()
    {
        return "Some String";
    }
    
    public string ThingString
    {
        get => "Some String";// comm
    }
}