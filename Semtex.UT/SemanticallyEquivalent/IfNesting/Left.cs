using System;

namespace Semtex.UT.ShouldPass.IfNesting;

public class Left
{
    public void Log(bool flag, bool flag2)
    {
        if (flag)
        {
            Console.WriteLine("flag is true");
            if (flag2)
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
    
    public void Log3(bool flag, bool flag2, int z,bool flag3)
    {
        if (flag3)
        {
            if (flag2)
            {
                int x = 1 + z;
                Console.Write(x + 1);
            }

            if (flag)
            {
                var x = z;
                Console.Write(x + 1);
            }
        }
    }

}