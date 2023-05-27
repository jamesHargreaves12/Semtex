namespace Semtex.UT.ShouldPass.UnnecessaryNullCheck;

public class Right
{
    public static string GetString(bool? flag)
    {
        if (flag.HasValue && flag.Value)
        {
            return "True";
        }

        return "null or False";
    }
}