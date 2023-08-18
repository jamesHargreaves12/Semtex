using System;
using System.IO;

namespace Semtex.UT.SemanticallyEquivalent.UsingBracketed;

public class Right
{
    public void M()
    {
        using var f = new FileStream("arosnt", FileMode.Open, FileAccess.Read);
        Console.WriteLine(f);
    }

}