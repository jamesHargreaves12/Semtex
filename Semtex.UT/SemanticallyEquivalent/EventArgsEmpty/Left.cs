using System;

namespace Semtex.UT.ShouldPass.EventArgsEmpty;

public class Left
{
    public static void Log(EventArgs e)
    {
        Console.WriteLine(e);
    }

    public void Call()
    {
        Log(new EventArgs());
    }

}