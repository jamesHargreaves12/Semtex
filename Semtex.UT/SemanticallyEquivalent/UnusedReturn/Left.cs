using System.IO;

namespace Semtex.UT.SemanticallyEquivalent.UnusedReturn;

public class Left
{
    public static void M()
    {
        Directory.GetParent(typeof(Directory).Assembly.Location)!.ToString();
    }
}