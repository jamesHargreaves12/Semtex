namespace Semtex.UT.SemanticallyEquivalent.FloatConst;

public class Left
{
    private const float WaitTime = 3.5F;
    private const double WaitTime2 = 3.5D;

    public static float GetWaitTime()
    {
        return WaitTime;
    }

    public static double GetWaitTime2()
    {
        return WaitTime2;
    }
}