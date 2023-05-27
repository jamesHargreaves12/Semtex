namespace Semtex.UT.ShouldPass.RedundantBoolean;

public class Right
{
    public static string Method(bool flag1, bool flag2,bool flag3)
    {
        if (flag1)
        {
            return "a";
        }

        if (!flag2)
        {
            return "b";
        }

        if (flag3)
        {
            return "d";
        }

        return "z";
    }

}