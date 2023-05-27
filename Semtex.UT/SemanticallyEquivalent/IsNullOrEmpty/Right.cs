namespace Semtex.UT.ShouldPass.IsNullOrEmpty;

public class Right
{
    public static string GetOrDefault(string? s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return "default";
        }

        return s;
    }

}