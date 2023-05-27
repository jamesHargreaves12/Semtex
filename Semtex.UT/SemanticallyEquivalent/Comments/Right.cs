namespace Semtex.UT.ShouldPass.Comments;

public class Right
{
    private readonly string _name;

    public Right(string name)
    {
        _name = name;
    }

    public string GiveMeTheNameWithSuffix(int suffix)
    {
        var newName = _name + suffix.ToString();
        return newName;
    }
}