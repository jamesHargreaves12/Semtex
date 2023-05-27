using System;

namespace Semtex.UT.SemanticallyEquivalent.Infinity;

public class Left
{
    private static float PosInf = float.PositiveInfinity; 
    private static float NegInf = float.NegativeInfinity; 
    private static double PosInfd = double.PositiveInfinity; 
    private static double NegInfd = double.NegativeInfinity; 
    public static void M(float f)
    {
        if(f == PosInf || f == NegInf )Console.WriteLine("Large abs value");
    }
    
    public static void M(double d)
    {
        if(d == PosInfd || d == NegInfd )Console.WriteLine("Large abs value");
    }
} 