using System;

namespace Semtex.UT.SemanticallyEquivalent.Maths;

public class Left
{
    public static int Three()
    {
        return 1 + 1 + 1;
    }
    
    public static bool OnlyThree(int x)
    {
        return x is BasicUtils.Three;
    }

    public static int Two()
    {
        return 3 - 1;
    }

    public static int One()
    {
        return 1 / 1;
    }

    public static int Zero()
    {
        return 4 * 0;
    }

    public static string GetNamePlusPrev()
    {
        return nameof(GetNamePlusPrev) + nameof(Zero);
    }

    private const float Pi = 3.14f;
    public static float PiSq()
    {
        return Pi*Pi;
    }

    public const double E = 2.7;
    public static double ECube()
    {
        return E*E*E;
    }

    public static double Value(double d)
    {
        if (d == Double.NaN)
        {
            return 3;
        }

        return d;
    }

}