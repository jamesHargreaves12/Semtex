using System.Text.RegularExpressions;

namespace Semtex.UT.ShouldPass.RegexInstanceOverStatic;

public class Right
{
    private static readonly Regex _regex = new Regex("[A-Z]*");
    public bool AllCaps(string x)
    {
        return _regex.IsMatch(x);
    }
}