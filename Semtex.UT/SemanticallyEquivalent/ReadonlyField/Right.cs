namespace Semtex.UT.ShouldPass.ReadonlyField;

public class Right
{
    private readonly int privateField;
    public int publicButPrivateSet { get; }

    public Right()
    {
        privateField = 0;
        publicButPrivateSet = 3;
    }
}