namespace Semtex.UT.ShouldPass.UnnecessaryNullCheck;

public class Left
{
    public static string GetString(bool? flag)
    {
        if (flag == true)
        {
            return "True";
        }

        return "null or False";
    }
}