using System;

namespace Semtex.UT.ShouldPass.RedundantDispose;

public class Right
{
    public static void DoSomething()
    {
        using (var e = new ExternalObj())
        {
            Console.WriteLine(e);
        }
    }

}