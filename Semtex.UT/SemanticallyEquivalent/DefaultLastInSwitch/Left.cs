namespace Semtex.UT.ShouldPass.DefaultLastInSwitch;

public class Left
{
    public static string GetString(int x)
    {
        switch (x)
        {
            case 1:
                return "One";
            default:
            case 2:
                return "More";
        }
    }

}