using System;

namespace Semtex.UT.NotSemanticallyEquivalent.Finally;

public class Right
{
    public static void DoSomething()
    {
        try
        {
            BasicUtils.UntrustedFunction();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            Console.WriteLine("Finally");
        }
    }
}