using System;

namespace Semtex.UT.ShouldPass.WhitespaceDifferences;

public class Left
{
    private readonly string _name;

    public Left(string name)
    {
        _name = name;
    }

    public string GiveMeTheNameWithSuffix(int suffix)
    {
        var newName = _name + suffix.ToString();
        Console.WriteLine($"Output = {newName}");
        return newName;
    }
}