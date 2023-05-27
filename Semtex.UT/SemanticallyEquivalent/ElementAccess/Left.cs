using System.Linq;

namespace Semtex.UT.ShouldPass.ElementAccess;

public class Left
{
    public static int First(int[] items)
    {
        return items.First();
    }
}