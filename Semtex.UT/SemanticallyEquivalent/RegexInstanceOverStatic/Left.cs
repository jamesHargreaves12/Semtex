using System.Text.RegularExpressions;

namespace Semtex.UT.ShouldPass.RegexInstanceOverStatic;

public class Left
{
    public bool AllCaps(string x)
    {
        return Regex.IsMatch(x,"[A-Z]*");
    }
}