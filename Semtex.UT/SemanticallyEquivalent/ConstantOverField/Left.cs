using System;
using Semtex.UT;

namespace Semtex.UT.ShouldPass.ConstantOverField;

public class Left
{
    private static readonly int _one = 1;
    private static Double Double = Double.Epsilon;

    public int GetOne()
    {
        Console.WriteLine(Double.Epsilon);
        return _one;
    }
}