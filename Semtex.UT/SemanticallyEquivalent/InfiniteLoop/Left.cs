namespace Semtex.UT.ShouldPass.InfiniteLoop;

public class Left
{
    public static void NeverFinish()
    {
        for (;;)
        {
        }
    }
}