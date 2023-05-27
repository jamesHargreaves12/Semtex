namespace Semtex.UT.ShouldPass.Comments;

public class Left // strange place to comment
{
    // Insightful comment
    /// <summary>
    /// _name field
    /// </summary>
    private readonly string _name;

    /// <summary>
    /// Constructor 
    /// </summary>
    /// <param name="name"></param>
    public Left(string name)
    {
        //Setting field
        _name = name;
    }

    /// <summary>
    /// Wow cool method
    /// </summary>
    /// <param name="suffix"></param>
    /// <returns></returns>
    public string GiveMeTheNameWithSuffix(int suffix)
    {
        // Complicated explanations
        var newName = _name + suffix.ToString();
        // give back the result to the caller
        return newName;
    }
    // Why would I add a comment down here?
}
// Or here!