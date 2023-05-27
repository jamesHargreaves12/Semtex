namespace Semtex.UT.SemanticallyEquivalent.Maths;

public class Right
{
    private const int OneValue = 1;
    public static int Three()
    {
        return 3;
    }
    public static bool OnlyThree(int x)
    {
        return x is 3;
    }

    public static int Two()
    {
        return 2;
    }
    public static int One()
    {
        return OneValue;
    }
    public static int Zero()
    {
        return 0;
    }
    
    public static string GetNamePlusPrev()
    {
        return "GetNamePlusPrevZero";
    }

    public static float PiSq()
    {
        return 3.14f*3.14f;
    }

    public const double E = 2.7;
    public static double ECube()
    {
        return 2.7*2.7*2.7;
    }

}


