namespace Semtex.UT.NotSemanticallyEquivalent.UsedPrivate;

public class Left
{
    public int GetSomethingHidden()
    {
        return SomethingHidden();
    }

    private int SomethingHidden()
    {
        return 1;
    }
}