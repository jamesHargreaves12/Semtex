namespace Semtex.UT.ShouldPass.StringBuilder;

public class Left
{
    public static string Build(string prefix)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(prefix + "value");
        return sb.ToString();
    }
}