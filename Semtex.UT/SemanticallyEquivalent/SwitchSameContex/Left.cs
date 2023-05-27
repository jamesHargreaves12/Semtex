namespace Semtex.UT.ShouldPass.SwitchSameContex;

public class Left
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
                break;
            case "c":
                break;
            default:
                isA = "nah";
                break;
        }

        return isA;
    }
}