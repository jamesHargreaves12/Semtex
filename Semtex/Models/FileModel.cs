
using System.Text.Json.Serialization;
using Semtex.Semantics;

namespace Semtex.Models;

public class FileModel
{
    [JsonIgnore]
    public readonly HashSet<MethodIdentifier>? SubsetOfMethodsThatAreNotEquivalent;

    public FileModel(string filepath, Status status, HashSet<MethodIdentifier>? subsetOfMethodsThatAreNotEquivalent = default)
    {
        if (status == Status.SubsetOfDiffEquivalent && subsetOfMethodsThatAreNotEquivalent is null or { Count: 0 })
        {
            throw new ArgumentException("If status is SomeMethodsEquivalent then you must also pass a list of methods");
        }

        SubsetOfMethodsThatAreNotEquivalent = subsetOfMethodsThatAreNotEquivalent;
        Filepath = filepath;
        Status = status;
    }

    public string Filepath { get; }
    public Status Status { get; }
}