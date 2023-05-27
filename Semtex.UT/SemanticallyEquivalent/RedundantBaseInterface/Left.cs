using System.Collections.Generic;

namespace Semtex.UT.ShouldPass.RedundantBaseInterface;

public class Left: List<int>, IEnumerable<int>
{
}