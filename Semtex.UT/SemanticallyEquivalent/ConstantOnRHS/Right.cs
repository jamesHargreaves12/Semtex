namespace Semtex.UT.ShouldPass.ConstantOnRHS;

public class Right
{
    public static string GetOrDefault(string? x)
    {
        if (x == null)
        {
            return "default";
        }

        return x;
    }
}