namespace Semtex.UT.ShouldPass.ExplicitObjectCreation;

public class Left
{
    public static Wrapper Wrapper = new(3);
    public static (bool, string[]) Pair = new(true, new string[] { "areistn" });
}