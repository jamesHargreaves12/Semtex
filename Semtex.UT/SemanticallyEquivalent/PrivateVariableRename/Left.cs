using System;

namespace Semtex.UT.SemanticallyEquivalent.EasyPrivateVariableRename;

public class Left
{
    private int XX = 1;
    private int YY {get;set;}
    private struct ZZ
    {
        
    }

    private void Printer(string x)
    {
        Console.WriteLine(x);
    }

    public void Something()
    {
        XX += 1;
    }

    public void EvenPlusOne()
    {
        Printer("arnesiirsnt");
        Console.WriteLine(XX);
        Console.WriteLine(YY);
        Console.WriteLine(new ZZ());
    }
}