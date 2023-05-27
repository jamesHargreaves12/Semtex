namespace Semtex.UT.ShouldPass.EmptyElse;

public class Left
{
    public static void DoSomething(bool flag)
    {
        if (flag)
        {
            BasicUtils.Add(1,2);
        }
        else
        {
        }
    } 
}