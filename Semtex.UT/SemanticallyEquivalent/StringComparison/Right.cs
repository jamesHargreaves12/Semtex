
namespace Semtex.UT.ShouldPass.StringComparison;

public class Right
{
    public static string EqualStrings(string l, string r)
    {
        if (string.Equals(l, r, System.StringComparison.OrdinalIgnoreCase))
        {
            return "Yes";
        }

        return "No";
    }
}