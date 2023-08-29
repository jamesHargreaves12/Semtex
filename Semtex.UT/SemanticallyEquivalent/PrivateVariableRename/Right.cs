using System;

namespace Semtex.UT.SemanticallyEquivalent.EasyPrivateVariableRename;

public class Right
{
    private int _xx = 1;
    private int Yy {get;set;}
    private struct Zz
    {
        
    }

    public void Something()
    {
        _xx += 1;
    }

    public void EvenPlusOne()
    {
        ConsoleWriter("arnesiirsnt");
        Console.WriteLine(_xx);
        Console.WriteLine(Yy);
        Console.WriteLine(new Zz());
    }
    
    private static void ConsoleWriter(string x)
    {
        Console.WriteLine(x);
    }

}