namespace Semtex.UT.ShouldPass.ExplicitlyTypedArray;

public class Right
{
    public static string Method()
    {
        var items = new string[] { "a" };
        return items[0];
    }
}