namespace Semtex.UT.ShouldPass.IsNullOrEmpty;

public class Left
{
    public static string GetOrDefault(string? s)
    {
        if (s == null || s == "")
        {
            return "default";
        }

        return s;
    }
}