using System.Runtime.CompilerServices;

namespace Semtex.UT.SemanticallyEquivalent.SuppressNullableWithReadonly;

public class Left
{
    int _a;
    int _b;
    void M(ref int x) {}
    public void M2() {M(ref _a!);}

    public ref int M3()
    {
        return ref Unsafe.Add(ref _b, 4);
    }
}