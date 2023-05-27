using System.Linq;

namespace Semtex.UT.ShouldPass.VarInsteadType;

public class Right
{
    public static string Wrap(int x)
    {
        Wrapper y = new Wrapper { X = x };
        var acc = 0; // I don't think the compiler can actually be certain what type this is due to the fact there are many number types.
        foreach (int v in Enumerable.Range(0, 10))
        {
            acc += v;
        }
        return $"{y.X}{acc}";
    }
}