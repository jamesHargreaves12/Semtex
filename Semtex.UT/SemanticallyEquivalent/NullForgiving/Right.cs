namespace Semtex.UT.ShouldPass.NullForgiving;

public class Right
{
    public static void DoNothing(string? input)
    {
    }

    public static void Caller(string? s)
    {
        DoNothing(s);
    }
}