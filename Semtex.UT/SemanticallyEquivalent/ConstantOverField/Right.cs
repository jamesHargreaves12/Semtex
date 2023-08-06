using System;

namespace Semtex.UT.ShouldPass.ConstantOverField;

public class Right
{
    private const int _one = 1;
    private static Double Double = Double.Epsilon;

    public int GetOne()
    {
        Console.WriteLine(Double);

        return _one;
    }
}