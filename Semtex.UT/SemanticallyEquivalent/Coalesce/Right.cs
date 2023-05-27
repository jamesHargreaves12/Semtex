namespace Semtex.UT.ShouldPass.CoalesceOverConditional;

public class Right
{
    public static string OrDefault(string? s)
    {
        return s ?? "default";
    }
    
    public static string OrDefaultTwo(string? s)
    {
        var x = s ?? "default";
        return x;
    }
    
    public static string OrDefaultThree(string? a, string b)
    {
        string z;
        z = a ?? b;

        return z;
    }
}