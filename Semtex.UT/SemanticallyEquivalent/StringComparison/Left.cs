namespace Semtex.UT.ShouldPass.StringComparison;

public class Left
{
    public static string EqualStrings(string l, string r)
    {
        if (l.ToLower() == r.ToLower())
        {
            return "Yes";
        }

        return "No";
    }
}