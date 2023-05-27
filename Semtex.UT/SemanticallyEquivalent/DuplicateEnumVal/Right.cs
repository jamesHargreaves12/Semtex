namespace Semtex.UT.ShouldPass.DuplicateEnumVal;

public class Right
{
    public enum Event
    {
        WIN = 0,
        LOSE = 1,
        DRAW = LOSE
    }
}
