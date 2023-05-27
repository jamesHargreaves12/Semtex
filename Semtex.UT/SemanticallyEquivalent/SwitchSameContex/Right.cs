namespace Semtex.UT.ShouldPass.SwitchSameContex;

public class Right
{
    public static string MagicA(string s)
    {
        var isA = "nope";
        switch (s)
        {
            case "a":
                isA = "a";
                break;
            case "b":
            case "c":
                break;
            default:
                isA = "nah";
                break;
        }

        return isA;
    }

}