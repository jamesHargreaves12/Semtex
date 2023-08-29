namespace Semtex.UT.ShouldPass.ExplicitObjectCreation;

public class Right
{
    public static Wrapper Wrapper = new Wrapper(3);
    public static (bool, string[]) Pair = new(true, new string[] { "areistn" });
}