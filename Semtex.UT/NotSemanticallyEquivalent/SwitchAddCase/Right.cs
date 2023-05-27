using System;

namespace Semtex.UT.NotSemanticallyEquivalent.SwitchAddCase;

public class Right
{
    public void Print(int i)
    {
        switch (i)
        {
            case 0:
                Console.WriteLine("Not a lot");
                break;
            case 1:
                Console.WriteLine("More");
                break;
            default:
                Console.WriteLine("Lots");
                break;
        }
    }

}