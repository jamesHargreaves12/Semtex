namespace Semtex.UT.ShouldPass.AddParentheses;

public class Right
{
    public static string BoolLogic(bool x, bool y, bool z)
    {
        if (x || y && z)
        {
            return "pass";
        }

        return "fail";

    }
}