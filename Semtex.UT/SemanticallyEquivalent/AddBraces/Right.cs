namespace Semtex.UT.ShouldPass.AddBracesIfMultiline;

public class Right
{
    public static int DoWork(int x, int y)
    {
        var z = 0;
        if (x < y)
        {
            z = BasicUtils.Add(
                x,
                y);
        }

        return z;
    }
}