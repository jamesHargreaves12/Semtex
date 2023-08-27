using Semtex.Models;

namespace Semtex;

public record UnsimplifiedFilesSummary
{
    public UnsimplifiedFilesSummary(HashSet<AbsolutePath> filepathsWithIfPreprocessor,
        HashSet<AbsolutePath> filepathsInProjThatFailedToCompile,
        HashSet<AbsolutePath> filepathsWhichUnableToFindProjFor,
        HashSet<AbsolutePath> filepathsInProjThatFailedToRestore)
    {
        FilepathsWithIfPreprocessor = filepathsWithIfPreprocessor;
        FilepathsInProjThatFailedToCompile = filepathsInProjThatFailedToCompile;
        FilepathsWhichUnableToFindProjFor = filepathsWhichUnableToFindProjFor;
        FilepathsInProjThatFailedToRestore = filepathsInProjThatFailedToRestore;
    }

    internal HashSet<AbsolutePath> FilepathsWithIfPreprocessor { get; }
    internal HashSet<AbsolutePath> FilepathsInProjThatFailedToCompile { get; }
    internal HashSet<AbsolutePath> FilepathsWhichUnableToFindProjFor { get; }
    public HashSet<AbsolutePath> FilepathsInProjThatFailedToRestore { get; }

    public static UnsimplifiedFilesSummary Empty()
    {
        return new UnsimplifiedFilesSummary(
            new HashSet<AbsolutePath>(),
            new HashSet<AbsolutePath>(),
            new HashSet<AbsolutePath>(),
            new HashSet<AbsolutePath>()
        );
    }

    public bool IsUnsimplified(AbsolutePath p)
    {
        return FilepathsWithIfPreprocessor.Contains(p) || FilepathsInProjThatFailedToCompile.Contains(p) ||
               FilepathsWhichUnableToFindProjFor.Contains(p) || FilepathsInProjThatFailedToRestore.Contains(p);
    }
}
