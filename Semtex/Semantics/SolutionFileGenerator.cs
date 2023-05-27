
using System.Text;

internal static class SolutionFileGenerator
{
    internal static string CreateSolutionText(List<string> projectPaths)
    {
        var solutionText = new StringBuilder();
        solutionText.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
        var projectGuids = new List<string>();
        foreach (var projectPath in projectPaths)
        {
            var projectFileName = Path.GetFileNameWithoutExtension(projectPath);
            var projectGuid = Guid.NewGuid().ToString("B").ToUpper();
            projectGuids.Add(projectGuid);
            solutionText.AppendLine($"Project(\"{projectGuid}\") = \"{projectFileName}\", \"{projectPath}\", \"{projectGuid}\"");
            solutionText.AppendLine("EndProject");
        }

        solutionText.AppendLine("Global");
        solutionText.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
        solutionText.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
        solutionText.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
        solutionText.AppendLine("\tEndGlobalSection");
        solutionText.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

        foreach (var projectGuid in projectGuids)
        {
            solutionText.AppendLine($"\t\t{projectGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
            solutionText.AppendLine($"\t\t{projectGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU");
            solutionText.AppendLine($"\t\t{projectGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU");
            solutionText.AppendLine($"\t\t{projectGuid}.Release|Any CPU.Build.0 = Release|Any CPU");
        }

        solutionText.AppendLine("\tEndGlobalSection");
        solutionText.AppendLine("\tGlobalSection(SolutionProperties) = preSolution");
        solutionText.AppendLine("\t\tHideSolutionNode = FALSE");
        solutionText.AppendLine("\tEndGlobalSection");
        solutionText.AppendLine("EndGlobal");

        return solutionText.ToString();
    }
}
