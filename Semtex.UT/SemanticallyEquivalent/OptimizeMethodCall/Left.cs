namespace Semtex.UT.ShouldPass.OptimizeMethodCall;

public class Left
{
    public int AreEqual(string s1, string s2)
    {
        return string.Compare(s1, s2, System.StringComparison.Ordinal);
    }
}