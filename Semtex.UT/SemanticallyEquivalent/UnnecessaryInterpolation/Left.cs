namespace Semtex.UT.ShouldPass.UnnecessaryInterpolation;

public class Left
{
    public static string FirstFive()
    {
        return $"abcde";
    }
    public static string Concat(string s1, string s2)
    {
        return $"{s1}{s2}";
    }
}