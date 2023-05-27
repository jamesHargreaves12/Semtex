using Xunit;

namespace Semtex.UT.SemanticallyEquivalent.CantUseBatchFixAll;

public class Right
{
    public static TheoryData<string[], int[]?, int[]?, int[]?, int[]?, int?[]?, int[]> M()
    {
        var l = nameof(Wrapper);
        var r = nameof(BasicOptions);
        return new()
        {
            { new string[] { l, l, r, r }, null, null, new int[] { 1 }, null, null, new int[] { 1, 3, 0, 2 } }, 
            { new string[] { l, l, l, r, r, r }, null, null, null, null, null, new int[] { 2 } },
        };
    }

}