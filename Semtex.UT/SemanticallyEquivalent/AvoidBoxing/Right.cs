namespace Semtex.UT.ShouldPass.AvoidBoxing;

public class Right
{
    public static string GetFormat()
    {
        var s = "x =";
        var i = 42;
        var w = new Wrapper(3);
        var st = new Struct("struct_name");
        return $"{s} {i.ToString()} {w.X.ToString()} {st.ToString()}";
    }
}