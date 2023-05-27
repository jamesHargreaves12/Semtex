namespace Semtex.UT.ShouldPass.IfToReturn;

public class Left
{
    public static string? Identity(string? val)
    {
        if (val != null)
        {
            return val;
        }
        else
        {
            return null;
        }
    }
}