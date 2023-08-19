using System;
using System.IO;

namespace Semtex.UT.SemanticallyEquivalent.UsingBracketed;

public class Left
{
    public void M()
    {
        Console.WriteLine("A");
        using (var f = new FileStream("arosnt", FileMode.Open, FileAccess.Read))
        {
            Console.WriteLine(f);
        }
    }
    
    public void M2()
    {
        Console.WriteLine("A");
        using (var f = new FileStream("arosnt", FileMode.Open, FileAccess.Read))
        using (var f2 = new FileStream("arosnt", FileMode.Open, FileAccess.Read))
        {
            Console.WriteLine(f);
            Console.WriteLine(f2);
        }
    }
}