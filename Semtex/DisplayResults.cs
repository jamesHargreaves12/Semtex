using System.Text;
using Semtex.Models;

namespace Semtex;

public static class DisplayResults
{
    internal static async Task<string> GetPrettySummaryOfResultsAsync(CommitModel result, GitRepo gitRepo, string? commitDisplayTitle=default)
    {
        var resultSummary = new StringBuilder();
        var commitDisplayName = commitDisplayTitle ?? await gitRepo.GetCommitOnelineDisplay(result.CommitHash).ConfigureAwait(false);

        resultSummary.AppendLine(result.SemanticallyEquivalent
            ? $"🎉 {commitDisplayName}: Is semantically equivalent ({result.ElapsedMilliseconds}ms)🎉"
            : $"❗ {commitDisplayName}: Could not be shown to be semantically equivalent ({result.ElapsedMilliseconds}ms)❗");

        var semEquiv = result.FileModels
            .Where(f => f.Status == Status.SemanticallyEquivalent)
            .ToList();
        AddSectionIfNotEmpty(
            resultSummary,
            semEquiv,
            "✅",
            "Semantically equivalent:"
        );

        var onlyRename = result.FileModels
            .Where(f => f.Status == Status.OnlyRename)
            .ToList();
        AddSectionIfNotEmpty(
            resultSummary,
            onlyRename,
            "✅",
            "Only Renamed:"
        );

        var notEquiv = result.FileModels   
            .Where(f => f.Status == Status.ContainsSemanticChanges)
            .ToList();
        AddSectionIfNotEmpty(
            resultSummary,
            notEquiv,
            "❌",
            "Contained semantic changes:"
        );
        
        var added = result.FileModels
            .Where(f => f.Status == Status.Added)
            .ToList();
        AddSectionIfNotEmpty(
            resultSummary,
            added,
            "❌",
            "Added:"
        );

        var removed = result.FileModels
            .Where(f => f.Status == Status.Removed)
            .ToList();
        AddSectionIfNotEmpty(
            resultSummary,
            removed,
            "❌",
            "Removed:"
        );

        
        var notCs = result.FileModels
            .Where(f => f.Status == Status.NotCSharp)
            .ToList();
        AddSectionIfNotEmpty(
            resultSummary,
            notCs,
            "❌",
            "Not C# so were not checked:"
        );
        
        var notCompile = result.FileModels
            .Where(f => f.Status == Status.ProjectDidNotCompile)
            .ToList();
        AddSectionIfNotEmpty(
            resultSummary,
            notCompile,
            "❌",
            "Projects failed to compile:"
        );
        
        var notRestore = result.FileModels
            .Where(f => f.Status == Status.ProjectDidNotRestore)
            .ToList();
        AddSectionIfNotEmpty(
            resultSummary,
            notRestore,
            "❌",
            "Projects failed to restore:"
        );
        
        var hasConditionalPreprocessor = result.FileModels
            .Where(f => f.Status == Status.HasConditionalPreprocessor)
            .ToList();
        AddSectionIfNotEmpty(
            resultSummary,
            hasConditionalPreprocessor,
            "❌",
            "Contained conditional preprocessors so were not checked:"
        );
        
        var unableToFindProj = result.FileModels
            .Where(f => f.Status == Status.UnableToFindProj)
            .ToList();
        AddSectionIfNotEmpty(
            resultSummary,
            unableToFindProj,
            "❌",
            "Unable to find .csproj file:"
        );

        return resultSummary.ToString();
    }

    private static void AddSectionIfNotEmpty(StringBuilder resultSummary, List<FileModel> fileModels, string emoji, string title)
    {
        if (!fileModels.Any()) return;
        resultSummary.AppendLine($"    {title}");
        foreach (var fp in fileModels)
        {
            resultSummary.AppendLine($"      {emoji} {fp.Filepath}");
        }
    }
}