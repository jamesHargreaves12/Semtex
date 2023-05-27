namespace Semtex.UT.ShouldPass.RedundantAs;

public class Left
{
    public string Identity(string s)
    {
        var s2 = s as string;
        return s2;
    }
}