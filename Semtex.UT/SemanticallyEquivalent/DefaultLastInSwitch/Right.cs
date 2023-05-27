namespace Semtex.UT.ShouldPass.DefaultLastInSwitch;

public class Right
{
    public static string GetString(int x)
    {
        switch (x)
        {
            case 1:
                return "One";
            case 2:
            default:
                return "More";
        }
    }
}