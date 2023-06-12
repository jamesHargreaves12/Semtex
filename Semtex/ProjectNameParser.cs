namespace Semtex;

public class ProjectNameParser
{
    public static string GetMoniker(string projectName)
    {
        return projectName.Split("(")[1].Trim(')');
    }
    
    public static string GetNameWithoutMoniker(string projectName)
    {
        return projectName.Split("(")[0];
    }
}