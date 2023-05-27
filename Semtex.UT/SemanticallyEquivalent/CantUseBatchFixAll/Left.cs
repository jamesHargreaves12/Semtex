using Xunit;

namespace Semtex.UT.SemanticallyEquivalent.CantUseBatchFixAll;

public class Left
{
    // Minimal example copied from https://github.com/jellyfin/jellyfin/blob/master/tests/Jellyfin.Providers.Tests/Manager/ProviderManagerTests.cs
    public static TheoryData<string[], int[]?, int[]?, int[]?, int[]?, int?[]?, int[]> M()
    {
        var l = nameof(Wrapper);
        var r = nameof(BasicOptions);
        return new()
        {
            { new[] { l, l, r, r }, null, null, new[] { 1 }, null, null, new[] { 1, 3, 0, 2 } },
            { new[] { l, l, l, r, r, r }, null, null, null, null, null, new[] { 2 } },
        };
    }
}