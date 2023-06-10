namespace Semtex.Models;

public enum Status
{
    SemanticallyEquivalent = 0,
    ContainsSemanticChanges = 1,
    NotCSharp = 2,
    HasConditionalPreprocessor = 3,
    ProjectDidNotCompile = 4,
    Added = 5, 
    Removed = 6,
    UnableToFindProj = 7,
    ProjectDidNotRestore = 8,
    OnlyRename = 9
}