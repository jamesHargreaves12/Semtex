namespace Semtex.UT.ShouldPass.HasFlag;

public class Left
{
    public bool IsA(BasicOptions options)
    {
        return (options & BasicOptions.A) != 0;
    }
}