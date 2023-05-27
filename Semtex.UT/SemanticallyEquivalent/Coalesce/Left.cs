namespace Semtex.UT.ShouldPass.CoalesceOverConditional;

public class Left
{
    public static string OrDefault(string? s)
    {
        return (s != null) ? s : "default";
    }
    
    public static string OrDefaultTwo(string? s)
    {
        var x = s;
        if (x == null)
        { 
            x = "default";
        }

        return x;
    }

    public static string OrDefaultThree(string? a, string b)
    {
        string z;
        if (a != null)
        {
            z = a;
        }
        else
        {
            z = b;
        }

        return z;
    }

}