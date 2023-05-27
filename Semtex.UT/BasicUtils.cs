
using System;

namespace Semtex.UT;

public static class BasicUtils
{
    public const int Three = 3;

    public static int Add(int x, int y)
    {
        if(x>0)
            return x + y;

        return y - x;
    }
    public static int AddWithLog(int x, int y)
    {
        Console.WriteLine(x);
        return x + y;
    }

    public static void UntrustedFunction()
    {
        throw new System.NotImplementedException();
    }
}

public interface IWrapper
{
    public void Log();
}

public class Wrapper: IWrapper
{
    public Wrapper()
    {

    }

    public Wrapper(int x)
    {
        X = x;
    }

    public void Log()
    {
        Console.WriteLine(X);
    }

    public Wrapper GetClone()
    {
        return new Wrapper(X);
    }

    public int X { get; set; }

    public static Wrapper operator+ (Wrapper w, Wrapper w2)
    {
        return new Wrapper(w.X + w2.X);
    }
}

public class WrapperSimple: IWrapper
{
    public void Log()
    {
        Console.WriteLine("Nothing");
    }
}


[Flags]
public enum BasicOptions
{
    A=0x1,
    B=0x2,
    C=0x4
}

public class ExternalObj : IDisposable
{
    public int Value = 1;
    public void Dispose()
    {
        Console.WriteLine("Bye");
    }
}

public struct Struct
{
    public Struct() : this("NoName")
    {
    }

    public Struct(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

public class Abc
{
    public virtual string MustImplement()
    {
        return "base";
    }

}  
