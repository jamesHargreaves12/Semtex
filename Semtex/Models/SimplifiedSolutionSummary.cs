using Microsoft.CodeAnalysis;

namespace Semtex.Models;


// I think Simplified Projects not just being a list if ProjectId is a mistake
public record SimplifiedSolutionSummary
{
    private readonly Solution? _sln;
    private readonly HashSet<ProjectId> _simplifiedProjectIds;
    public readonly UnsimplifiedFilesSummary UnsimplifiedFilesSummary;

    public SimplifiedSolutionSummary(Solution? sln, HashSet<ProjectId> simplifiedProjectIds, UnsimplifiedFilesSummary unsimplifiedFilesSummary)
    {
        _sln = sln;
        _simplifiedProjectIds = simplifiedProjectIds;
        UnsimplifiedFilesSummary = unsimplifiedFilesSummary;
    }

    public static SimplifiedSolutionSummary Empty()
    {
        return new SimplifiedSolutionSummary(null, new HashSet<ProjectId>(), UnsimplifiedFilesSummary.Empty());
    }

    public IEnumerable<Project>? SimplifiedProjects => _sln?.Projects.Where(p => _simplifiedProjectIds.Contains(p.Id));
}