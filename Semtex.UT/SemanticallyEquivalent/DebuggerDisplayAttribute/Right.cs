namespace Semtex.UT.ShouldPass.DebuggerDisplayAttribute;

[System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
public class Right
{
    [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            return ToString();
        }
    }
}