using System;

namespace Semtex.UT.NotSemanticallyEquivalent.SwitchAddCase;

public class Left
{
    public void Print(int i)
    {
        switch (i)
        {
            case 0:
                Console.WriteLine("Not a lot");
                break;
            default:
                Console.WriteLine("Lots");
                break;
        }
    }
}