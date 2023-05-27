namespace Semtex.UT.ShouldPass.StringBuilder;

public class Right
{
    public static string Build(string prefix)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(prefix).Append("value");
        return sb.ToString();
    }
}