using System;

namespace Semtex.UT.ShouldPass.EventArgsEmpty;

public class Right
{
    public static void Log(EventArgs e)
    {
        Console.WriteLine(e);
    }

    public void Call()
    {
        Log(EventArgs.Empty);
    }
}