using System.Linq;

namespace Semtex.UT.ShouldPass.VarInsteadType;

public class Left
{
    public static string Wrap(int x)
    {
        var y = new Wrapper { X = x };
        var acc = 0;
        foreach (var v in Enumerable.Range(0, 10))
        {
            acc += v;
        }
        return $"{y.X}{acc}";
    }
}