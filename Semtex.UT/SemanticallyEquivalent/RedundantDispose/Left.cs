using System;

namespace Semtex.UT.ShouldPass.RedundantDispose;

public class Left
{
    public static void DoSomething()
    {
        using (var e = new ExternalObj())
        {
            Console.WriteLine(e);
            e.Dispose();
        }
    }
}