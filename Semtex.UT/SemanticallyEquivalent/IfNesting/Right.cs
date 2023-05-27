using System;

namespace Semtex.UT.ShouldPass.IfNesting;

public class Right
{
    public void Log(bool flag, bool flag2)
    {
        if (!flag) return;
        
        Console.WriteLine("flag is true");
        
        if (flag2)
        {
            Console.WriteLine("And so is flag2");
        }
    }
    
    // Just added to ensure that it doesn't error.
    public bool Log2(bool flag)
    {
        {
            int x;
        }

        if (flag)
        {
            var x = 1;
            return true;
        }

        return false;

    }

}