namespace Semtex.UT.ShouldPass.HasFlag;

public class Right
{
    public bool IsA(BasicOptions options)
    {
        return options.HasFlag(BasicOptions.A);
    }
}