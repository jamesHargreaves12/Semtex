namespace Semtex.UT.ShouldPass.StringEmptyCheck;

public class Right
{
    public static string IsEmpty(string s)
    {
        if (s?.Length == 0)
        {
            return "Yes";
        }

        return "No";
    }
}