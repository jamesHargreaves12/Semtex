namespace Semtex.UT.ShouldPass.MethodChaining;

public class Left
{
    public static string Build(string s1, string s2, bool s3)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(s1);
        sb.Append(s2);
        sb.Append(s3);
        return sb.ToString();
    }
}