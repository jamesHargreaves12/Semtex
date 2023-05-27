using Microsoft.CodeAnalysis;

namespace Semtex.ProjectFinder;

public class ComputeDocumentToProjMapping
{
    public static Dictionary<string, string[]> ComputeDocumentToProjectMapping(Solution sln)
    {
        var result = new Dictionary<string, string[]>();
        foreach (var proj in sln.Projects)
        {
            if (proj.FilePath is null) continue;

            foreach (var doc in proj.Documents)
            {
                if(doc.FilePath is null) continue;

                if (result.ContainsKey(doc.FilePath))
                    result[doc.FilePath] = result[doc.FilePath].Append(proj.FilePath).ToArray();

                result[doc.FilePath] = new string[]{ proj.FilePath };
            }
        }

        return result;
    }
}