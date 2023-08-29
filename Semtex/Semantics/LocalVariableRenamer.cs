using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.AccessControl;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.Extensions.Logging;
using Semtex.Logging;

namespace Semtex.Semantics;

public class LocalVariableRenamer
{
    private static readonly ILogger<LocalVariableRenamer> Logger = SemtexLog.LoggerFactory.CreateLogger<LocalVariableRenamer>();
    
    internal static async Task<List<(ISymbol left, string right)>> GetProposedLocalVariableRenames(MethodDeclarationSyntax left, MethodDeclarationSyntax right,
        SemanticModel leftSemanticModel, SemanticModel rightSemanticModel, Document leftDocument, Document rightDocument)
    {
        var sw = Stopwatch.StartNew();
        if (left.Body is null || right.Body is null)
            return new List<(ISymbol, string)>();
        var leftDeclaredVariables = leftSemanticModel.AnalyzeDataFlow(left.Body).VariablesDeclared;
        var rightDeclaredVariables = rightSemanticModel.AnalyzeDataFlow(right.Body).VariablesDeclared;
        Logger.LogInformation(SemtexLog.GetPerformanceStr("GetVariableIdentifiers", sw.ElapsedMilliseconds));

        var leftOccursSingleTime = leftDeclaredVariables.GroupBy(x => x.Name)
            .Where(x => x.Count() == 1)
            .Select(x => x.Single())
            .ToList();
        var rightOccursSingleTime = rightDeclaredVariables.GroupBy(x => x.Name)
            .Where(x => x.Count() == 1)
            .Select(x => x.Single())
            .ToList();

        var inBoth = leftOccursSingleTime.Select(x => x.Name).Intersect(rightOccursSingleTime.Select(x => x.Name)).ToHashSet();
        var leftCandidates = leftOccursSingleTime.Where(x => !inBoth.Contains(x.Name));
        var rightCandidates = rightOccursSingleTime.Where(x => !inBoth.Contains(x.Name));
        
        var leftVariableIdentifiers = await Task.WhenAll(leftCandidates.Select(x => GetSymbolIdentifier(leftSemanticModel, leftDocument, x))).ConfigureAwait(false);
        var rightVariableIdentifiers = await Task.WhenAll(rightCandidates.Select(x => GetSymbolIdentifier(rightSemanticModel, rightDocument, x))).ConfigureAwait(false);
        
        // Group by type + ref count and look at any see if there are any obvious matches.
        var leftMapping = leftVariableIdentifiers
            .ToLookup(x => (x.typeName, x.referenceCount));
        var rightMapping = rightVariableIdentifiers
            .ToLookup(x => (x.typeName, x.referenceCount));
        var renames = new List<(ISymbol, string)>();
        foreach (var rightGroup in rightMapping)
        {
            var leftGroup = leftMapping[rightGroup.Key];

            var leftVars = leftGroup.Select(x => x.name).ToList();
            var rightVars = rightGroup.Select(x => x.name).ToList();
            
            if (leftVars.Count == 1 && rightVars.Count == 1)
            {
                var leftSymbol = leftDeclaredVariables.Single(v => v.Name == leftVars.First());
                renames.Add((leftSymbol, rightVars.First()));
            }
        }

        return renames;
    }
    
    private static async Task<(string name, string typeName, int referenceCount)> GetSymbolIdentifier(SemanticModel semanticModel, Document document, ISymbol symbol)
    {
        var docsToSearch = new HashSet<Document>() { document }.ToImmutableSortedSet();
        switch (symbol)
        {
            case ILocalSymbol localSymbol:
            {
                var references = await SymbolFinder
                    .FindReferencesAsync(symbol, document.Project.Solution, documents: docsToSearch)
                    .ConfigureAwait(false);
                var identifier = (localSymbol.Name, localSymbol.Type.Name, references.First().Locations.Count());
                return identifier;
            }
            // Can occur inside lambdas
            case IParameterSymbol parameterSymbol:
            {
                var references = await SymbolFinder
                    .FindReferencesAsync(symbol, document.Project.Solution, documents: docsToSearch)
                    .ConfigureAwait(false);
                var identifier = (parameterSymbol.Name, $"{nameof(IParameterSymbol)}_{parameterSymbol.Type.Name}",
                    references.First().Locations.Count());
                return identifier;
            }
            case IRangeVariableSymbol rangeVariableSymbol:
            {
                var references = await SymbolFinder
                    .FindReferencesAsync(symbol, document.Project.Solution, documents: docsToSearch)
                    .ConfigureAwait(false);
                var node = await rangeVariableSymbol.DeclaringSyntaxReferences.First().GetSyntaxAsync()
                    .ConfigureAwait(false);

                var type = node is FromClauseSyntax fromClause
                    ? semanticModel.GetTypeInfo(fromClause.Expression).Type?.ToDisplayString()
                    : null;
                Logger.LogInformation("Unable to get type info for {NameofIRangeVariableSymbol} {Symbol}, will use UNKNOWN",
                    nameof(IRangeVariableSymbol), symbol);

                // The type will actually be the type of the 
                var identifier = (rangeVariableSymbol.Name, $"{nameof(IRangeVariableSymbol)}_{type ?? "UNKNOWN"}",
                    references.First().Locations.Count());
                return identifier;
            }
            default:
                throw new NotImplementedException(
                    $"This shouldn't be hit because any declared symbol inside a MethodDeclarationSyntax should by a local variable {symbol}");
        }
    }
}