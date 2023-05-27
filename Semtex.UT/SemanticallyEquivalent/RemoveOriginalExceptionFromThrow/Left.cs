using System;

namespace Semtex.UT.ShouldPass.RemoveOriginalExceptionFromThrow;

public class Left
{
    public static void DoSomething()
    {
        try
        {
            BasicUtils.Add(1, 2);
        }
        catch(Exception e)
        {
            Console.WriteLine("Hit Error");
            throw e;
        }
    }
}