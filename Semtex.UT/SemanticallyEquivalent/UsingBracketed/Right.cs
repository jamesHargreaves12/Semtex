using System;
using System.IO;

namespace Semtex.UT.SemanticallyEquivalent.UsingBracketed;

public class Right
{
    public void M()
    {
        Console.WriteLine("A");
        using var f = new FileStream("arosnt", FileMode.Open, FileAccess.Read);
        var x = 1;
        Console.WriteLine(f);
    }
    public void M2()
    {
        Console.WriteLine("A");
        using var f = new FileStream("arosnt", FileMode.Open, FileAccess.Read);
        using var f2 = new FileStream("arosnt", FileMode.Open, FileAccess.Read);
        Console.WriteLine(f);
        Console.WriteLine(f2);
    }
    
    public void M3()
    {
        using (var f = new FileStream("arosnt", FileMode.Open, FileAccess.Read))
        {
            Console.WriteLine(f);
        }
        using (var f = new FileStream("arosnt", FileMode.Open, FileAccess.Read))
        {
            Console.WriteLine(f);
        }
    }
    public void M4(int x)
    {
        switch (x)
        {
            case 1:
                using (var f = new FileStream("arosnt", FileMode.Open, FileAccess.Read))
                {
                    Console.WriteLine(f);
                }
                using (var f = new FileStream("arosnt", FileMode.Open, FileAccess.Read))
                {
                    Console.WriteLine(f);
                }
                break;
            default:
                Console.WriteLine(2);
                break;
        }
    }
    
    public void M3(ExternalObj x)
    {
        using (var f = new FileStream("arosnt", FileMode.Open, FileAccess.Read))
        using (x)
        {
            Console.WriteLine(x);
            Console.WriteLine(f);
        }
    }
}