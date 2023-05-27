namespace Semtex.UT.ShouldPass.AutoImplementedProperty;

public class Left
{
    private string _internalString;
    
    public string ExternalString
    {
        get { return _internalString; }
        set { _internalString = value; }
    }
}