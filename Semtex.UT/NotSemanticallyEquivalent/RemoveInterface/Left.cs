using System;

namespace Semtex.UT.NotSemanticallyEquivalent.RemoveInterface;

public class Left: IDisposable
{
    public void Dispose()
    {
        Console.WriteLine("Disposing");
    }
}