namespace Semtex.UT.SemanticallyEquivalent.SupressNullableWarnings;

public class Left
{
    public static object M(object o)
    {
        return o!;
    }
}