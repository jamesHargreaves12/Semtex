namespace Semtex.UT.NotSemanticallyEquivalent.DiffField;

public class Left
{
    private int _field1;
    private int _field2;

    public Left(int field1, int field2)
    {
        this._field1 = field1;
        this._field2 = field2;
    }

    public void MakeChange1()
    {
        _field1 = 1;
    }
    public void MakeChange2()
    {
        _field2 = 2;
    }

    public int GetValue()
    {
        return _field1 + _field2;
    }
}