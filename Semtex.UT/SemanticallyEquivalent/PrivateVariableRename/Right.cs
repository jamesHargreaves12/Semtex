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
        Console.WriteLine(_xx);
        Console.WriteLine(Yy);
        Console.WriteLine(new Zz());
    }

}