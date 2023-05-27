namespace Semtex.UT.SemanticallyEquivalent.PolymorphicNullCoalesce;

public class Right
{
    public interface IBase
    {
    }

    public class A: IBase
    {
    }
    
    private class B: IBase
    {
    }

    public static IBase M(A? a)
    {
        return (IBase?)a ?? new B();
    }

}