namespace Semtex.UT.NotSemanticallyEquivalent.DiffField;

public class Right
{
    private int _field1;
    private int _field2;

    public Right(int field1, int field2)
    {
        this._field1 = field1;
        this._field2 = field2;
    }

    public void MakeChange1()
    {
        _field2 = 1;
    }
    public void MakeChange2()
    {
        _field1 = 2;
    }

    public int GetValue()
    {
        return _field1 + _field2;
    }
}