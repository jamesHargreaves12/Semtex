namespace Semtex.UT.ShouldPass.ReadonlyField;

public class Left
{
    private int privateField;
    public int publicButPrivateSet { get; private set; }

    public Left()
    {
        privateField = 0;
        publicButPrivateSet = 3;
    }
}