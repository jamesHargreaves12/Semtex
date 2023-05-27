using System.IO;

namespace Semtex.UT.ShouldPass.SimplityNestedUsing;

public class Right
{
    public static void WriteToFile()
    {
        using (var fs = new FileStream("path", FileMode.OpenOrCreate))
        using (var sw = new StreamWriter(fs))
        {
            sw.Write("SomeData\n");
        }
        BasicUtils.Add(1, 2);
    }
}