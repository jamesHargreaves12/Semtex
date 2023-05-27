using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.Extensions.Logging;
using Semtex.Logging;

namespace Semtex.Semantics;

public class LocalVariableRenamer
{
    private static readonly ILogger<LocalVariableRenamer> Logger = SemtexLog.LoggerFactory.CreateLogger<LocalVariableRenamer>();
    internal static async Task<List<(string left, string right)>> GetProposedLocalVariableRenames(MethodDeclarationSyntax left, MethodDeclarationSyntax right,
        SemanticModel leftSemanticModel, SemanticModel rightSemanticModel, Document leftDocument, Document rightDocument)
    {
        var sw = Stopwatch.StartNew();
        var leftVariableIdentifiers = await GetVariableIdentifiers(left, leftSemanticModel, leftDocument).ConfigureAwait(false);
        var rightVariableIdentifiers = await GetVariableIdentifiers(right, rightSemanticModel, rightDocument).ConfigureAwait(false);
        Logger.LogInformation(SemtexLog.GetPerformanceStr(nameof(GetVariableIdentifiers), sw.ElapsedMilliseconds));
        // strip out any local variables that occur twice.
        var leftOccursMultipleTimes = leftVariableIdentifiers
            .GroupBy(x => x.name)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key).ToHashSet();
        var leftUniqueVariableIdentifiers = leftVariableIdentifiers.Where(x => !leftOccursMultipleTimes.Contains(x.name));
        
        var rightOccursMultipleTimes = leftVariableIdentifiers
            .GroupBy(x => x.name)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key).ToHashSet();
        var rightUniqueVariableIdentifiers = rightVariableIdentifiers.Where(x => !rightOccursMultipleTimes.Contains(x.name));

        // Group by type + ref count and look at any see if there are any obvious matches.
        var leftMapping = leftUniqueVariableIdentifiers
            .ToLookup(x => (x.typeName, x.referenceCount));
        var rightMapping = rightUniqueVariableIdentifiers
            .ToLookup(x => (x.typeName, x.referenceCount));
        var renames = new List<(string, string)>();
        foreach (var rightGroup in rightMapping)
        {
            var leftGroup = leftMapping[rightGroup.Key];

            var leftVars = leftGroup.Select(x => x.name).ToList();
            var rightVars = rightGroup.Select(x => x.name).ToList();

            var inBoth = leftVars.Intersect(rightVars).ToHashSet();
            var leftUniqueVars = leftVars.Where(v => !inBoth.Contains(v)).ToList();
            var rightUniqueVars = rightVars.Where(v => !inBoth.Contains(v)).ToList();

            if (leftUniqueVars.Count == 1 && rightUniqueVars.Count == 1)
            {
                renames.Add((leftUniqueVars.First(), rightUniqueVars.First()));
            }
        }

        return renames;
    }

    private static async Task<List<(string name, string typeName, int referenceCount)>> GetVariableIdentifiers(MethodDeclarationSyntax method,
        SemanticModel semanticModel, Document document)
    {
        var body = method.Body;
        if(body == null)
        {
            throw new NotImplementedException("TODO");
        }

        var docsToSearch = new HashSet<Document>() { document }.ToImmutableSortedSet();
        var dataFlowAnalysis = semanticModel.AnalyzeDataFlow(body);
        var declared = dataFlowAnalysis.VariablesDeclared;

        var results = new List<(string name, string typeName, int referenceCount)>();
        foreach (var symbol in declared)
        {
            if (symbol is ILocalSymbol localSymbol)
            {
                var references = await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, documents: docsToSearch)
                    .ConfigureAwait(false);
                var identifier = (localSymbol.Name, localSymbol.Type.Name, references.First().Locations.Count());
                results.Add(identifier);
                continue;
            }
            
            if (symbol is IParameterSymbol parameterSymbol) // Can occur inside lambdas
            {
                var references = await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, documents: docsToSearch)
                    .ConfigureAwait(false);
                var identifier = (parameterSymbol.Name, $"{nameof(IParameterSymbol)}_{parameterSymbol.Type.Name}", references.First().Locations.Count());
                results.Add(identifier);
                continue;
            }

            if (symbol is IRangeVariableSymbol rangeVariableSymbol)
            {
                var references = await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, documents: docsToSearch)
                    .ConfigureAwait(false);
                var node = await rangeVariableSymbol.DeclaringSyntaxReferences.First().GetSyntaxAsync()
                    .ConfigureAwait(false);
                
                var type = node is FromClauseSyntax fromClause 
                    ? semanticModel.GetTypeInfo(fromClause.Expression).Type?.ToDisplayString()
                    : null;
                Logger.LogInformation("Unable to get type info for {NameofIRangeVariableSymbol} {Symbol}, will use UNKNOWN", nameof(IRangeVariableSymbol), symbol);

                // The type will actually be the type of the 
                var identifier = (rangeVariableSymbol.Name, $"{nameof(IRangeVariableSymbol)}_{type??"UNKNOWN"}", references.First().Locations.Count());
                results.Add(identifier);
                continue;
            }

            throw new NotImplementedException(
                    $"This shouldn't be hit because any declared symbol inside a MethodDeclarationSyntax should by a local variable {symbol}");
        }
        return results;
    }

}