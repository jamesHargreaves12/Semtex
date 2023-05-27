using System;

namespace Semtex.UT.ShouldPass.Enum;

[Flags]
public enum Right
{
    One=1,
    Two=1 << 1,
    Four=1 << 2,
    Eight=1 << 3
}