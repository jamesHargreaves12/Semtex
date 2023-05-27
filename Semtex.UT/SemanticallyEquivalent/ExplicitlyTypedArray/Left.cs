namespace Semtex.UT.ShouldPass.ExplicitlyTypedArray;

public class Left
{
    public static string Method()
    {
        var items = new[] { "a" };
        return items[0];
    }
}