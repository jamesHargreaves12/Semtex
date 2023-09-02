using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;
using Roslynator.CSharp.Analysis;
using Roslynator.CSharp.Analysis.MakeMemberReadOnly;
using Roslynator.CSharp.Analysis.MarkLocalVariableAsConst;
using Roslynator.CSharp.Analysis.UnusedMember;
using Roslynator.CSharp.Analysis.UsePatternMatching;
using RoslynatorCsDiagnosticFixers;
using RoslynatorRcsCodeFixes;
using RoslynCsCodeFixes;
using Semtex.Logging;
using Semtex.Models;
using SemtexAnalyzers;

namespace Semtex.Semantics;

public sealed class SafeAnalyzers
{
    private static readonly ILogger<SafeAnalyzers> Logger = SemtexLog.LoggerFactory.CreateLogger<SafeAnalyzers>();

    internal static readonly Dictionary<string, CodeFixProvider> CodeFixProviders =
        new[]
            {
                RoslynatorRcsCodeFixProviders.SupportedCodeFixes,
                RoslynatorCsCodeFixProviders.SupportedCodeFixes,
                RoslynCsCodeFixProviders.SupportedCodeFixes,
                SemtexCodeFixProviders.SupportedCodeFixes
            }
            .SelectMany(x => x)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

    private static readonly ImmutableArray<DiagnosticAnalyzer> Analyzers = new HashSet<DiagnosticAnalyzer>()
    {
        new UnusedMemberAnalyzer(),
        new RemoveUnnecessaryElseAnalyzer(),
        new UseVarInsteadOfExplicitTypeWhenTypeIsObviousAnalyzer(),
        new AddBracesAnalyzer(),
        new SimplifyNestedUsingStatementAnalyzer(),
        new UseExplicitTypeInsteadOfVarInForEachAnalyzer(),
        new UsePredefinedTypeAnalyzer(),
        new UseExplicitlyOrImplicitlyTypedArrayAnalyzer(),
        new UseBlockBodyOrExpressionBodyAnalyzer(),
        new SimplifyNullableOfTAnalyzer(),
        new LambdaExpressionAnalyzer(),
        new RemoveUnnecessaryBracesInSwitchSectionAnalyzer(),
        new RemoveRedundantParenthesesAnalyzer(),
        new BooleanLiteralAnalyzer(),
        new RemoveRedundantCommaInInitializerAnalyzer(),
        new RemoveEmptyStatementAnalyzer(),
        new AttributeArgumentListAnalyzer(),
        new RemoveEmptyElseClauseAnalyzer(),
        new RemoveEmptyInitializerAnalyzer(),
        new RemoveEnumDefaultUnderlyingTypeAnalyzer(),
        new RemoveOriginalExceptionFromThrowStatementAnalyzer(),
        new AnonymousMethodAnalyzer(),
        new AddOrRemoveParenthesesWhenCreatingNewObjectAnalyzer(),
        new DeclareEachAttributeSeparatelyAnalyzer(),
        new AvoidUsageOfUsingAliasDirectiveAnalyzer(),
        new UseCompoundAssignmentAnalyzer(),
        new MergeIfWithNestedIfAnalyzer(),
        new RemoveEmptyFinallyClauseAnalyzer(),
        new SimplifyLogicalNegationAnalyzer(),
        new RemoveUnnecessaryCaseLabelAnalyzer(),
        new RemoveRedundantBaseConstructorCallAnalyzer(),
        new IfStatementAnalyzer(),
        new RemoveRedundantConstructorAnalyzer(),
        new InvocationExpressionAnalyzer(),
        new UseEmptyStringLiteralOrStringEmptyAnalyzer(),
        new SplitVariableDeclarationAnalyzer(),
        new SimplifyNullCheckAnalyzer(),
        new UseAutoPropertyAnalyzer(),
        new UseUnaryOperatorInsteadOfAssignmentAnalyzer(),
        new UseHasFlagMethodOrBitwiseOperatorAnalyzer(),
        new ConstantValuesShouldBePlacedOnRightSideOfComparisonsAnalyzer(),
        new SimplifyConditionalExpressionAnalyzer(),
        new UnnecessaryInterpolationAnalyzer(),
        new RemoveEmptyDestructorAnalyzer(),
        new UseStringIsNullOrEmptyMethodAnalyzer(),
        new RemoveRedundantDelegateCreationAnalyzer(),
        new LocalDeclarationStatementAnalyzer(),
        new AddParenthesesWhenNecessaryAnalyzer(),
        new InlineLocalVariableAnalyzer(),
        new UseCoalesceExpressionAnalyzer(),
        new RemoveRedundantFieldInitializationAnalyzer(),
        new RemoveRedundantOverridingMemberAnalyzer(),
        new RemoveRedundantDisposeOrCloseCallAnalyzer(),
        new RemoveRedundantStatementAnalyzer(),
        new MergeSwitchSectionsAnalyzer(),
        new RemoveRedundantAsOperatorAnalyzer(),
        new UseConditionalAccessAnalyzer(),
        new RemoveRedundantCastAnalyzer(),
        new SortEnumMembersAnalyzer(),
        new UseStringLengthInsteadOfComparisonWithEmptyStringAnalyzer(),
        new AbstractTypeShouldNotHavePublicConstructorsAnalyzer(),
        new EnumShouldDeclareExplicitValuesAnalyzer(),
        new MakeMemberReadOnlyAnalyzer(),
        new SimplifyLazyInitializationAnalyzer(),
        new UseIsOperatorInsteadOfAsOperatorAnalyzer(),
        new RemoveRedundantAsyncAwaitAnalyzer(),
        new UnnecessaryAssignmentAnalyzer(),
        new RemoveRedundantBaseInterfaceAnalyzer(),
        new InvocationExpressionAnalyzer(),
        new UseConstantInsteadOfFieldAnalyzer(),
        new RemoveRedundantAutoPropertyInitializationAnalyzer(),
        new JoinStringExpressionsAnalyzer(),
        new UnnecessaryUsageOfVerbatimStringLiteralAnalyzer(),
        new ImplementExceptionConstructorsAnalyzer(),
        new UseExclusiveOrOperatorAnalyzer(),
        new CallExtensionMethodAsInstanceMethodAnalyzer(),
        new InvocationExpressionAnalyzer(),
        new AvoidBoxingOfValueTypeAnalyzer(),
        new UnnecessaryNullCheckAnalyzer(),
        new UseEventArgsEmptyAnalyzer(),
        new OrderNamedArgumentsAnalyzer(),
        new UseAnonymousFunctionOrMethodGroupAnalyzer(),
        new ReduceIfNestingAnalyzer(),
        new OrderTypeParameterConstraintsAnalyzer(),
        new RemoveRedundantAssignmentAnalyzer(),
        new UnnecessaryInterpolatedStringAnalyzer(),
        new BinaryOperatorAnalyzer(),
        new UnnecessaryUnsafeContextAnalyzer(),
        new ConvertInterpolatedStringToConcatenationAnalyzer(),
        new SimplifyCodeBranchingAnalyzer(),
        new UsePatternMatchingInsteadOfIsAndCastAnalyzer(),
        new UsePatternMatchingInsteadOfAsAndNullCheckAnalyzer(),
        // new MarkTypeWithDebuggerDisplayAttributeAnalyzer(), causes bug with check https://github.com/jellyfin/jellyfin.git f3c57e6a0ae015dc51cf548a0380d1bed33959c2 --all-ancestors
        new MakeClassSealedAnalyzer(),
        new UnnecessaryExplicitUseOfEnumeratorAnalyzer(),
        new UseShortCircuitingOperatorAnalyzer(),
        new EnumSymbolAnalyzer(),
        new ConditionalExpressionAnalyzer(),
        new UseForStatementInsteadOfWhileStatementAnalyzer(),
        new RefReadOnlyParameterAnalyzer(),
        new DefaultExpressionAnalyzer(),
        new UnnecessaryNullForgivingOperatorAnalyzer(),
        new UseImplicitOrExplicitObjectCreationAnalyzer(),
        new RemoveUnnecessaryBracesAnalyzer(),
        new NormalizeUsageOfInfiniteLoopAnalyzer(),
        new NormalizeFormatOfEnumFlagValueAnalyzer(),
        new ConstantValueAnalyzer(),
        new LogTemplateParamsAnalyzer(),
        new CanBeMadeStaticAnalyzer(),
        new UsingStatementAnalyzer()
    }.ToImmutableArray();

    private static int GetPriority(string descriptorId)
    {
        return descriptorId switch
        {
            "RCS1220" => 1, // need to be applied before RCS1208
            "CS8019" => -1, // apply this last as theres a good chance there will be more unused usings.
            _ => 0
        };
    }

    private static readonly HashSet<string> DiagnosticToApplyEvenIfNotInChangeMap = new HashSet<string>()
    {
        "RCS1213", // Unused Member
    };
    internal static async Task<Solution> Apply(Solution sln, ProjectId projId, List<DocumentId> documentIds,
        AbsolutePath? analyzerConfigPath, Dictionary<DocumentId, HashSet<MethodIdentifier>> changedMethodsMap, IProgress<double> progress)
    {
        // Clone the set so that any edits don't effect caller.
        var currentDocumentIds = new HashSet<DocumentId>(documentIds);
        var currentSolution = await AnalyzerConfigOverwrite.ReplaceAnyAnalyzerConfigDocuments(sln, projId, currentDocumentIds, analyzerConfigPath).ConfigureAwait(false);

        Logger.LogDebug("Starting Applying Diagnostic Fixes");

        // We tried to fix it but it didn't change the solution so we pull it from the set of diagnostics that will be considered for this file.
        var diagnosticsThatDidntMakeFix = new HashSet<(string filename, string diagnosticId)>();
        var analyzerOptions = new AnalyzerOptions(
            additionalFiles: EmptyAdditionalFiles,
            optionsProvider: currentSolution.GetProject(projId)!.AnalyzerOptions.AnalyzerConfigOptionsProvider
        );

        foreach (var _ in Enumerable.Range(0, 1000)) // If we hit 1000 we have almost certainly hit an inf loop
        {
            progress.Report(1 - (double)currentDocumentIds.Count / documentIds.Count);
            var project = currentSolution.GetProject(projId)!;

            var relevantDiagnostics =
                await CompileAndGetRelevantDiagnostics(project, currentDocumentIds, analyzerOptions).ConfigureAwait(false);

            relevantDiagnostics = relevantDiagnostics
                .Where(d => CodeFixProviders.ContainsKey(d.Descriptor.Id))
                .Where(d => !diagnosticsThatDidntMakeFix.Contains((d.Location.GetLineSpan().Path, d.Id)))
                .ToList();

            if (!relevantDiagnostics.Any())
            {
                break;
            }

            var sw = Stopwatch.StartNew();
            // Apply one diagnostic for each file that we care about. This makes the assumption that changes to one file wont invalidate the diagnostic in another.
            foreach (var documentId in currentDocumentIds)
            {
                var document = currentSolution
                    .GetDocument(documentId);

                if (document is null)
                {
                    Logger.LogWarning("Unable to find document in solution, skipping");
                    currentDocumentIds.Remove(documentId);
                    continue; // The .cs file is not actually compiled by the project this can happen if the file is a test case for something that acts on .cs files
                }

                var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
                var groupedDiagnostics = relevantDiagnostics
                    .Where(d => document.FilePath == d.Location.GetLineSpan().Path)
                    .Where(d => !changedMethodsMap.ContainsKey(documentId)
                               || IsDiagnosticsInChangedMethod(root!, d, changedMethodsMap[documentId])
                               || DiagnosticToApplyEvenIfNotInChangeMap.Contains(d.Descriptor.Id))
                    .GroupBy(d => d.Descriptor.Id)
                    .OrderByDescending(g => (GetPriority(g.Key), g.Count()))
                    .ToList();

                if (!groupedDiagnostics.Any())
                {
                    // (Optimization) Remove it from the set that we are looking from analyzers in
                    currentDocumentIds.Remove(documentId);
                    continue;
                }
                var diagnosticsToApply = groupedDiagnostics.First();
                var descriptorId = diagnosticsToApply.Key;

                Logger.LogDebug("Fixing {DescriptorId} x {N} on {LineSpan}", descriptorId, diagnosticsToApply.Count(),
                    diagnosticsToApply.First().Location.GetLineSpan());

                var fixProvider = CodeFixProviders[descriptorId];
                var nextSolution = await MakeCodeFixForAllDiagnostic(
                    document,
                    descriptorId,
                    diagnosticsToApply,
                    fixProvider).ConfigureAwait(false);
                if (nextSolution != currentSolution)
                {
                    currentSolution = nextSolution;
                }
                else
                {
                    diagnosticsThatDidntMakeFix.Add((document.FilePath!, descriptorId));
                }
            }

            Logger.LogDebug(SemtexLog.GetPerformanceStr(nameof(MakeCodeFixForAllDiagnostic), sw.ElapsedMilliseconds));
        }

        return currentSolution;
    }

    private static bool IsDiagnosticsInChangedMethod(SyntaxNode root, Diagnostic diagnostic,
        HashSet<MethodIdentifier> changedMethods)
    {
        var node = root.FindNode(diagnostic.Location.SourceSpan);
        while (true)
        {
            switch (node)
            {
                // need other cases here e.g. getters
                case MethodDeclarationSyntax methodDeclarationSyntax:
                    {
                        var methodIdentifier = SemanticSimplifier.GetMethodIdentifier(methodDeclarationSyntax);
                        return changedMethods.Contains(methodIdentifier);
                    }
                case ClassDeclarationSyntax or CompilationUnitSyntax or NamespaceDeclarationSyntax:
                    return true;
                case null:
                    return true;
                default:
                    node = node.Parent;
                    break;
            }
        }
    }

    private static readonly ImmutableArray<AdditionalText> EmptyAdditionalFiles = Array.Empty<AdditionalText>().ToImmutableArray();
    private static async Task<IEnumerable<Diagnostic>> CompileAndGetRelevantDiagnostics(Project proj, IEnumerable<DocumentId> documentIds, AnalyzerOptions analyzerOptions)
    {
        var compilation = await proj.GetCompilationAsync().ConfigureAwait(false);
        var stopwatch = Stopwatch.StartNew();

        var compilationWithAnalyzers = compilation!
            .WithAnalyzers(
                Analyzers,
                options: analyzerOptions);

        // These diagnostics come from the inbuilt analyzers.
        var diagnostics = new List<Diagnostic>(compilation!.GetDiagnostics());

        // These diagnostics come from the manually added Analyzers.
        var stopwatch2 = Stopwatch.StartNew();
        var documents = documentIds.Select(docId => proj.GetDocument(docId)!);
        var enumerable = documents.ToList();
        var tasks = enumerable.Select(d => GetManuallyAddedAnalyzerDiagnostics(d, compilationWithAnalyzers, compilation));
        var finishedTasks = await Task.WhenAll(tasks).ConfigureAwait(false);
        foreach (var ds in finishedTasks)
        {
            diagnostics.AddRange(ds);
        }

        Logger.LogDebug(SemtexLog.GetPerformanceStr("CompilationAndDiagnostics", stopwatch.ElapsedMilliseconds));
        Logger.LogDebug("{Percent}% from custom analyzers)", (int)(stopwatch2.ElapsedMilliseconds / (float)stopwatch.ElapsedMilliseconds * 100));

        var analyzerErrors = diagnostics
            .Where(d => d.Descriptor.Id == "AD0001")
            .Select(d => d.GetMessage())
            .ToList();
        if (analyzerErrors.Any())
        {
            Logger.LogDebug("Some analyzer's failed to be computed");
            foreach (var e in analyzerErrors)
            {
                Logger.LogDebug(e);
            }

            throw new SemtexCompileException(new AbsolutePath(proj.FilePath!), $"Analyzer execution threw exception {analyzerErrors.First()}");
        }

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            Logger.LogError("Error diagnostics found:");
            foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Take(10))
            {
                Logger.LogError($"{diagnostic.Id}({diagnostic.Location.SourceSpan}) with message {diagnostic.GetMessage()}");
            }

            var firstIssue = diagnostics.First(d => d.Severity == DiagnosticSeverity.Error);
            var sourceWithIssue = firstIssue.Location.SourceTree;
            Logger.LogDebug(sourceWithIssue!.ToString());
            throw new SemtexCompileException(new AbsolutePath(proj.FilePath!), $"Compile Failed {firstIssue}");
        }

        var documentPaths = enumerable.Select(d => d.FilePath!).ToHashSet();
        return diagnostics.Where(d => documentPaths.Contains(d.Location.GetLineSpan().Path));
    }

    private static async Task<ImmutableArray<Diagnostic>> GetManuallyAddedAnalyzerDiagnostics(Document document,
        CompilationWithAnalyzers compilationWithAnalyzers, Compilation compilation)
    {
        var syntaxTree = await document.GetSyntaxTreeAsync();
        // Note that this should now be easy to only apply the analyzers to certain files.
        return await compilationWithAnalyzers.GetAnalyzerSemanticDiagnosticsAsync(
            compilation.GetSemanticModel(syntaxTree!), filterSpan: null, cancellationToken: default);
    }


    private static async Task<Solution> MakeCodeFixForDiagnostic(Document curDoc, Diagnostic diagnostic,
        CodeFixProvider fixProvider)
    {
        List<CodeAction> actions = new();

        var context = new CodeFixContext(
            curDoc,
            diagnostic,
            (a, _) => actions.Add(a),
            default
        );

        await fixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        // should we throw if actions is the empty list?
        var action = actions.First();

        var operations = await action.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);

        return operations
            .OfType<ApplyChangesOperation>()
            .Single()
            .ChangedSolution;


    }


    private static readonly List<string> CannotFixAllDiagnosticIds = new List<string>()
    {
        "RCS1056", // Since fixing the first might change the fix you need to apply for the second its just safest to ignore this for now.
        "RCS1220", // Adds variables and is not smart enough to ensure they are distinct, TODO fix the code fix provider.
    };
    private static async Task<Solution> MakeCodeFixForAllDiagnostic(Document document,
        // this arg should not exist = simplify the interface
        string diagnosticDescriptorId, IEnumerable<Diagnostic> diagnostics, CodeFixProvider fixProvider)
    {
        // applying overlapping diagnostics will almost certainly fail.
        var nonOverlappingDiagnostic = new List<Diagnostic>();
        foreach (var diag in diagnostics)
        {
            if (nonOverlappingDiagnostic.Any(x => x.Location.SourceSpan.OverlapsWith(diag.Location.SourceSpan)))
                continue;
            nonOverlappingDiagnostic.Add(diag);
        }

        if (CannotFixAllDiagnosticIds.Contains(diagnosticDescriptorId))
        {
            Logger.LogDebug("Only fixing first due to diagnostic id");
            return await MakeCodeFixForDiagnostic(document, nonOverlappingDiagnostic.First(d => d.Descriptor.Id == diagnosticDescriptorId), fixProvider)
                .ConfigureAwait(false);
        }

        // Compute the equivalence key of the first diagnostic
        var sw = Stopwatch.StartNew();
        var codeActions = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            nonOverlappingDiagnostic.First(),
            (a, _) => codeActions.Add(a),
            default
        );
        await fixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        if (sw.ElapsedMilliseconds > 500)
            Logger.LogDebug(SemtexLog.GetPerformanceStr(nameof(fixProvider.RegisterCodeFixesAsync), sw.ElapsedMilliseconds));

        if (!codeActions.Any())
        {
            Logger.LogDebug("The code fix for diagnostic with Id {Id} did not raise any CodeActions", diagnosticDescriptorId);
            return document.Project.Solution;
        }

        var equivalenceKey = codeActions.First().EquivalenceKey!;

        // Apply the all diagnostic with the same the equivalence key
        // This causes issues. https://github.com/dotnet/roslyn/issues/59130 need to have a think about what the best solution is here. Step 1 should be learning more about this DocumentBasedCodeFixProvider.
        var fixAllProvider = fixProvider.GetFixAllProvider() ?? WellKnownFixAllProviders.BatchFixer;
        var fixAllContext = new FixAllContext(
            document,
            fixProvider,
            FixAllScope.Document,
            equivalenceKey,
            new[] { diagnosticDescriptorId },
            new SingleDocumentFixAllDiagnosticProvider(nonOverlappingDiagnostic, document.Id),
            default);


        try
        {
            sw.Restart();
            var fixAll = await fixAllProvider.GetFixAsync(fixAllContext).ConfigureAwait(false);

            if (sw.ElapsedMilliseconds > 500)
                Logger.LogDebug(SemtexLog.GetPerformanceStr(nameof(fixAllProvider.GetFixAsync), sw.ElapsedMilliseconds));

            if (fixAll != null)
            {
                sw.Restart();
                var operations =
                    await fixAll.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);

                if (sw.ElapsedMilliseconds > 500)
                    Logger.LogDebug(SemtexLog.GetPerformanceStr(nameof(fixAll.GetOperationsAsync), sw.ElapsedMilliseconds));

                return operations
                    .OfType<ApplyChangesOperation>()
                    .Single()
                    .ChangedSolution;
            }
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to apply fixes. {E}", e);
            var source = await document.GetSyntaxTreeAsync();
            Logger.LogDebug(source!.ToString());
            throw;
        }

        Logger.LogDebug("Unable to FixAll falling back to just fixing the first");

        return await MakeCodeFixForDiagnostic(document, nonOverlappingDiagnostic.First(d => d.Descriptor.Id == diagnosticDescriptorId),
            fixProvider).ConfigureAwait(false);

    }
}
