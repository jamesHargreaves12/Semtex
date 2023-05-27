namespace Semtex.UT.ShouldPass.MemberHidesInheritedMember;

public class Left: Wrapper
{
    public new int x;

    public Left(int x)
    {
        this.x = x;
    }
}