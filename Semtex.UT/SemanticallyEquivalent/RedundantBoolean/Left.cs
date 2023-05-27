namespace Semtex.UT.ShouldPass.RedundantBoolean;

public class Left
{
    public static string Method(bool flag1, bool flag2,bool flag3)
    {
        if (flag1 == true)
        {
            return "a";
        }

        if (flag2 != true)
        {
            return "b";
        }

        if (!!flag3)
        {
            return "d";
        }

        return "z";
    }
}