using System.Diagnostics;
using Semtex.Rewriters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.Extensions.Logging;
using Roslynator.CSharp;
using Semtex.Logging;
using Semtex.Models;

namespace Semtex.Semantics;

internal class SemanticSimplifier
{
    private static readonly ILogger<SemanticSimplifier> Logger =
        SemtexLog.LoggerFactory.CreateLogger<SemanticSimplifier>();

    internal static async Task<Solution> GetSolutionWithFilesSimplified(
        Solution sln, List<ProjectId> projectIds, Dictionary<AbsolutePath, HashSet<AbsolutePath>> projectToFilesMap, AbsolutePath? analyzerConfigPath,
        Dictionary<AbsolutePath, HashSet<string>> changedMethodsMap)
    {
        foreach (var projId in projectIds) // This could 100% be parallelized for speed - for now won't do this as the logging becomes more difficult.
        {
            var proj = sln.GetProject(projId)!;
            Logger.LogInformation("Processing {ProjName}", proj.Name);
            var docsToSimplify = projectToFilesMap[new AbsolutePath(proj.FilePath!)];

            // Simplify docs
            sln = await SafeAnalyzers.Apply(sln, projId, docsToSimplify, analyzerConfigPath, changedMethodsMap).ConfigureAwait(false);
            sln = await ApplyRewriters(sln, projId, docsToSimplify).ConfigureAwait(false);
        }

        return sln;
    }


    private static List<CSharpSyntaxRewriter> _rewriters = new()
    {
        new RemoveTriviaRewriter(),
        new ConsistentOrderRewriter(),
        new RemoveSuppressNullableWarningRewriter(),
        new ApplySimplificationServiceRewriter(),
        new TrailingCommaRewriter()
    };
    private static async Task<Solution> ApplyRewriters(Solution sln, ProjectId projectId,
        HashSet<AbsolutePath> docsToSimplify)
    {
        var sw = Stopwatch.StartNew();
        var docsToSimplifyString = docsToSimplify.Select(d => d.Path).ToHashSet(); 
        var simplifiedDocs = sln.GetProject(projectId)!.Documents
            .Where(d => docsToSimplifyString.Contains(d.FilePath!));

        foreach (var doc in simplifiedDocs)
        {
            var rootNode = (await doc.GetSyntaxRootAsync().ConfigureAwait(false))!;
            var compilation = await doc.Project.GetCompilationAsync().ConfigureAwait(false); // Should we just reuse this on subsequent loops?
            var semanticModel = compilation!.GetSemanticModel(rootNode.SyntaxTree);

            var renamablePrivateSymbolsWalker = new AllRenameablePrivateSymbols(semanticModel);
            renamablePrivateSymbolsWalker.Visit(rootNode);
            // TODO there is no need to keep this as any resembelance to what was there before. We should probably just make it semtex_{i} or something
            var renameConfig = renamablePrivateSymbolsWalker.PrivateSymbols
                .ToDictionary(s => s.Name, s => (s, $"p_{s.Name.ToLower().Trim('_')}")); 

            rootNode = new RenameSymbolRewriter(semanticModel, renameConfig).Visit(rootNode);
            foreach (var rewriter in _rewriters)
            {
                rootNode = rewriter.Visit(rootNode);
            }
            var normalizedWhiteSpace = rootNode.NormalizeWhitespace();
            sln = sln.WithDocumentSyntaxRoot(doc.Id, normalizedWhiteSpace);
            
            var simplifiedDoc = await Simplifier.ReduceAsync(sln.GetDocument(doc.Id)!,Simplifier.Annotation).ConfigureAwait(false);
            sln = sln.WithDocumentSyntaxRoot(doc.Id, (await simplifiedDoc.GetSyntaxRootAsync().ConfigureAwait(false))!);
        }
        
        Logger.LogInformation(SemtexLog.GetPerformanceStr(nameof(ApplyRewriters), sw.ElapsedMilliseconds));
        return sln;
    }

    /// <summary>
    /// TODO improve this.
    /// </summary>
    /// <param name="method"></param>
    /// <returns></returns>
    public static string GetMethodIdentifier(MethodDeclarationSyntax method)
    {
        var overloadIdentifier = string.Join("_",
            method.ParameterList.Parameters.Select(x => x.Type!.ToString()).ToArray());
        return $"{method.Identifier}_{overloadIdentifier}";
    }

    // TODO add an all symbols and only apply the renaming if they wont conflict.
    public class AllRenameablePrivateSymbols : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;

        public AllRenameablePrivateSymbols(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        public readonly List<ISymbol> PrivateSymbols = new List<ISymbol>();

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node.Modifiers.All(m =>
                    !m.IsKind(SyntaxKind.PublicKeyword)
                    && !m.IsKind(SyntaxKind.ProtectedKeyword) 
                    && !m.IsKind(SyntaxKind.InternalKeyword))
                && (node.Parent is ClassDeclarationSyntax cds && cds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                    || node.Parent is StructDeclarationSyntax sds && sds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                    || node.Parent is RecordDeclarationSyntax rds && rds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                ))
            {
                var symbol = _semanticModel.GetDeclaredSymbol(node);
                if(symbol is not null)
                    PrivateSymbols.Add(symbol);
            }

            base.VisitClassDeclaration(node);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (node.Modifiers.All(m =>
                    !m.IsKind(SyntaxKind.PublicKeyword)
                    && !m.IsKind(SyntaxKind.ProtectedKeyword) 
                    && !m.IsKind(SyntaxKind.InternalKeyword))
                && (node.Parent is ClassDeclarationSyntax cds && cds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                    || node.Parent is StructDeclarationSyntax sds && sds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                    || node.Parent is RecordDeclarationSyntax rds && rds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))) 
                && node.Declaration.Variables is [var declaration])
            {
                
                var symbol = _semanticModel.GetDeclaredSymbol(declaration);
                if(symbol is not null)
                    PrivateSymbols.Add(symbol);
            }

            base.VisitFieldDeclaration(node);
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)) 
                && node.FirstAncestor(SyntaxKind.ClassDeclaration) is ClassDeclarationSyntax cds && cds.Modifiers.All(m=>!m.IsKind(SyntaxKind.PartialKeyword)))
            {
                var symbol = _semanticModel.GetDeclaredSymbol(node);
                if(symbol is not null)
                    PrivateSymbols.Add(symbol);
            }

            base.VisitStructDeclaration(node);
        }

        public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            if (node.Modifiers.All(m =>
                    !m.IsKind(SyntaxKind.PublicKeyword)
                    && !m.IsKind(SyntaxKind.ProtectedKeyword) 
                    && !m.IsKind(SyntaxKind.InternalKeyword))
                && (node.Parent is ClassDeclarationSyntax cds && cds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                    || node.Parent is StructDeclarationSyntax sds && sds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                    || node.Parent is RecordDeclarationSyntax rds && rds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                ))
            {
                PrivateSymbols.Add(_semanticModel.GetDeclaredSymbol(node)!);
            }

            base.VisitRecordDeclaration(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (node.Modifiers.All(m =>
                    !m.IsKind(SyntaxKind.PublicKeyword)
                    && !m.IsKind(SyntaxKind.ProtectedKeyword) 
                    && !m.IsKind(SyntaxKind.InternalKeyword))
                && (node.Parent is ClassDeclarationSyntax cds && cds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                    || node.Parent is StructDeclarationSyntax sds && sds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                    || node.Parent is RecordDeclarationSyntax rds && rds.Modifiers.All(m => !m.IsKind(SyntaxKind.PartialKeyword))
                ))
            {
                var symbol = _semanticModel.GetDeclaredSymbol(node);
                if (symbol is not null)
                    PrivateSymbols.Add(symbol);
            }

            base.VisitPropertyDeclaration(node);
        }

        // Not doing method as we don't want to change the identifier
    }
}