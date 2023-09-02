using Microsoft.CodeAnalysis;
using Semtex.Models;

namespace Semtex.ProjectFinder;

public class ComputeDocumentToProjMapping
{
    public static Dictionary<AbsolutePath, AbsolutePath[]> ComputeDocumentToProjectMapping(Solution sln)
    {
        var result = new Dictionary<AbsolutePath, AbsolutePath[]>();
        foreach (var proj in sln.Projects)
        {
            if (proj.FilePath is null) continue;

            foreach (var doc in proj.Documents)
            {
                if (doc.FilePath is null) continue;

                var docFilePath = new AbsolutePath(doc.FilePath);
                var projFilePath = new AbsolutePath(proj.FilePath);

                if (result.ContainsKey(docFilePath))
                    result[docFilePath] = result[docFilePath].Append(projFilePath).ToArray();

                result[docFilePath] = new[] { projFilePath };
            }
        }

        return result;
    }
}