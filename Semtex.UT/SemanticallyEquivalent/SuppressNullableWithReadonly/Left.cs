namespace Semtex.UT.SemanticallyEquivalent.SuppressNullableWithReadonly;

public class Left
{
    int _a;
    void M(ref int x) {}
    public void M2() {M(ref _a!);} 
}