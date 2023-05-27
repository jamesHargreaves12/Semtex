namespace Semtex.UT.SemanticallyEquivalent.PolymorphicNullCoalesce;

public class Left
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
        return a != null ? a : new B();
    }
}