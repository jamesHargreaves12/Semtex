namespace Semtex.UT.ShouldPass.StringEmptyCheck;

public class Left
{
    public static string IsEmpty(string s)
    {
        if (s == "")
        {
            return "Yes";
        }

        return "No";
    }
}