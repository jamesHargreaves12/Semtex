namespace Semtex.UT.ShouldPass.ForVsWhile;

public class Left
{
    public static void CallN(int n)
    {
        int i = 0;
        while (i < n)
        {
            BasicUtils.Add(3, 4);
            i++;
        }
    }
}