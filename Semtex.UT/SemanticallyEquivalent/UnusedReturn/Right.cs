using System.IO;

namespace Semtex.UT.SemanticallyEquivalent.UnusedReturn;

public class Right
{
    // This is currently handled correctly but this test can act as a nice Repro for me.
    public static void M()
    {
        var x = Directory.GetParent(typeof(Directory).Assembly.Location)!.ToString();
    }
    
}