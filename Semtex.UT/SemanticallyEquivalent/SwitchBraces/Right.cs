using System;

namespace Semtex.UT.ShouldPass.SwitchBraces;

public class Right
{
    public static int BadFib(int x)
    {
        var answer = 1;
        switch (x)
        {
            case 1:
            case 0:
                break;
            default:
                answer = BadFib(x - 1) + BadFib(x - 2);
                break;
        }

        return answer;
    }
    
    // Just here to confirm doesnt error
    public static int M(int x)
    {
        switch (x)
        {
            case 1:
            case 0:
            {
                var y = BasicUtils.Add(1,2);
                Console.WriteLine(y);
                break;
            }
            default:
            {
                var y = BasicUtils.Add(1,3);
                Console.WriteLine(y);
                break;
            }
        }
        return 1;
    }
    public static int M2(int x)
    {
        switch (x)
        {
            case 1:
            case 0:
            {
                var y = BasicUtils.Add(1,2);
                Console.WriteLine(y);
                break;
            }
            default:
            if(x >3){
                var y = BasicUtils.Add(1,3);
                Console.WriteLine(y);
            }
            break;
        }
        return 1;
    }

}