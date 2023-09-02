using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Semtex.Logging;
using Semtex.Models;

namespace Semtex.Semantics;

public sealed class AnalyzerConfigOverwrite
{
    private static readonly ILogger<AnalyzerConfigOverwrite> Logger = SemtexLog.LoggerFactory.CreateLogger<AnalyzerConfigOverwrite>();

    private const string START_OF_CONFIG_FILE = """
is_global = true #https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files
roslynator_array_creation_type_style = explicit
roslynator_body_style = block
roslynator_object_creation_parentheses_style = include
roslynator_empty_string_style = literal
roslynator_enum_has_flag_style = operator
roslynator_object_creation_type_style = explicit
roslynator_use_var_instead_of_implicit_object_creation = true
roslynator_infinite_loop_style = while
roslynator_enum_flag_value_style = decimal_number
generated_code = false # The fix all provider will not fix all on code which has generated code commnet. We will only call fix all if the file is checked into git and so we want to ignore this check.
""";

    /// <summary>
    /// By default we want to control which analyzers are applied. To achieve this we strip out any existing
    /// AnalyzerConfig documents in the project and replace it with a new one that is controlled by us.
    /// 
    /// Applying the RCS Analyzers to all files in the solution can have a non zero cost. So as an optimisation
    /// we first suppress all these analyzers at a global level and then reenable them for each of the files with diffs
    /// in. This is done through the analyzer config file.
    /// </summary>

    internal static async Task<Solution> ReplaceAnyAnalyzerConfigDocuments(Solution sln, ProjectId projId, IEnumerable<DocumentId> changedDocumentIds, AbsolutePath? analyzerConfigPath)
    {
        var project = sln.GetProject(projId)!;
        // Strip out all existing config docs.
        var whitelistConfigDocumentName = $"{project.Name}.GeneratedMSBuildEditorConfig.editorconfig";
        var configDocuments = project.AnalyzerConfigDocuments
            .Where(x => x.Name != whitelistConfigDocumentName)
            .ToList();

        Logger.LogDebug(
            $"Stripping out Existing config documents {string.Join(",", configDocuments.Select(c => c.FilePath))}");
        var newSln = sln.RemoveAnalyzerConfigDocuments(configDocuments.Select(d => d.Id).ToImmutableArray());

        string configText;
        if (analyzerConfigPath is not null)
        {
            Logger.LogDebug("Adding {AnalyzerConfigPath} to the proj", analyzerConfigPath.Path);
            configText = await File.ReadAllTextAsync(analyzerConfigPath.Path).ConfigureAwait(false);
        }
        else
        {
            Logger.LogDebug("Adding a dynamically generated analyzer config to the project");
            var analyzerConfigFolder = Directory.GetParent(typeof(AnalyzerConfigOverwrite).Assembly.Location)!.ToString();
            configText = START_OF_CONFIG_FILE;

            analyzerConfigPath = new AbsolutePath(Path.Join(Path.GetDirectoryName(project.FilePath), ".analyzerconfig"));
            configText += GetAnalyzerConfigOnlyForFilesThatChanged(changedDocumentIds.Select(project.GetDocument));
        }

        // Add in a the analyzer config as if it was in the same folder as the project.
        newSln = newSln.AddAnalyzerConfigDocument(DocumentId.CreateNewId(projId), ".analyzerconfig", SourceText.From(configText), filePath: analyzerConfigPath.Path);
        return newSln;
    }

    private static string GetAnalyzerConfigOnlyForFilesThatChanged(IEnumerable<Document?> documents)
    {
        var suppressAnalyzerLines = GetAnalyzerConfigLines("none");
        var warningLines = GetAnalyzerConfigLines("warning");
        var result = new StringBuilder();
        result.Append((string?)suppressAnalyzerLines);
        result.AppendLine();
        foreach (var document in documents)
        {
            if (document?.FilePath is null)
                continue;

            result.AppendLine($"[{document.FilePath}]");
            result.Append((string?)warningLines);
            result.AppendLine();
        }

        return result.ToString();
    }

    private static string GetAnalyzerConfigLines(string severity)
    {
        var analyzerLinesBuilder = new StringBuilder();
        foreach (var diagId in SafeAnalyzers.CodeFixProviders.Keys)
        {
            if (diagId.StartsWith("RCS") || diagId.StartsWith("semtex"))
            {
                analyzerLinesBuilder.AppendLine($"dotnet_diagnostic.{diagId}.severity = {severity}");
            }
        }

        return analyzerLinesBuilder.ToString();
    }
}