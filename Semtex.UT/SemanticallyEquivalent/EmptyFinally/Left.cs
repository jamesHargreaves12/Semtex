namespace Semtex.UT.ShouldPass.EmptyFinally;

public class Left
{
    public void DoSomething()
    {
        try
        {
            BasicUtils.UntrustedFunction();
        }
        finally
        {
        }
    }
}