using Microsoft.CodeAnalysis;

namespace Semtex.Models;


// I think Simplified Projects not just being a list if ProjectId is a mistake
public record SimplifiedSolutionSummary(Solution? Sln, HashSet<ProjectId> SimplifiedProjectIds, UnsimplifiedFilesSummary UnsimplifiedFilesSummary)
{
    public static SimplifiedSolutionSummary Empty()
    {
        return new SimplifiedSolutionSummary(null, new HashSet<ProjectId>(), UnsimplifiedFilesSummary.Empty());
    }

    public IEnumerable<Project>? SimplifiedProjects => Sln?.Projects.Where(p => SimplifiedProjectIds.Contains(p.Id));
}