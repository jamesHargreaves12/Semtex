namespace Semtex.UT.ShouldPass.OptimizeMethodCall;

public class Right
{
    public int AreEqual(string s1, string s2)
    {
        return string.CompareOrdinal(s1, s2);
    }
}