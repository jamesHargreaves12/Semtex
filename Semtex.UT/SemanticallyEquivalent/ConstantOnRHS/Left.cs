namespace Semtex.UT.ShouldPass.ConstantOnRHS;

public class Left
{
    public static string GetOrDefault(string? x)
    {
        if (null == x)
        {
            return "default";
        }

        return x;
    }
}