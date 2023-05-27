namespace Semtex.UT.ShouldPass.ForVsWhile;

public class Right
{
    public static void CallN(int n)
    {
        for (int i = 0; i < n; i++)
        {
            BasicUtils.Add(3, 4);
        }
    }
}