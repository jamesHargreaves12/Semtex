using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Semtex.Semantics;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Semtex.Logging;
using Semtex.Models;

namespace Semtex.UT;

/// <summary>
/// There might looks like theres a lot going on in this class but really its al just setup to ensure that
/// <see cref="ShouldFindSemanticallyEquivalent"/> and <see cref="ShouldNotFindSemanticallyEquivalent"/>>
/// Run with the correct setting set of files
/// </summary>
public class SemanticEquivalenceTests
{
    private const string SemanticEquivalentLocation = "./SemanticallyEquivalent";
    private const string NotSemanticallyEquivalentDirectory = "./NotSemanticallyEquivalent";
    
    // <NonSemanticallyEquivalentDirectories Start>
   private static readonly string[] NonSemanticallyEquivalentDirectories = {
        "AccessModifierClass",
        "AccessModifierMethod",
        "AccessModifierProperty",
        "AccessModifierStaticField",
        "AccessModifierStaticMethod",
        "AddSideEffect",
        "BaseConstuctorCall",
        "CallDifferentFunction",
        "ChangeArgument",
        "ConstantVsAssignment",
        "Constructor",
        "DefaultValue",
        "Destructor",
        "DifferentException",
        "DiffField",
        "DiffProperty",
        "DiffSubType",
        "Enum",
        "Finally",
        "Goto",
        "IfElse",
        "IncrementOperator",
        "InvertingIf",
        "Linq",
        "Maths",
        "MoreLoop",
        "MultipleNamespaces",
        "Namespace",
        "NewException",
        "NewPublicMethod",
        "NullCheck",
        "NullCoalesce",
        "ParamType",
        "Partial",
        "PublicClassName",
        "PublicMethodArgs",
        "PublicMethodName",
        "RecordVsClass",
        "Ref",
        "RemoveInterface",
        "ReorderSideEffects",
        "ReorderWithOverloadedOperator",
        "ReturnType",
        "ReturnValue",
        "Sealed",
        "SemanticBrakets",
        "SemanticReordering",
        "StaticInitializer",
        "StructVsRecord",
        "StuffInRegion",
        "SwitchAddCase",
        "Tertiary",
        "Tuple",
        "UsedPrivate",
        "Using"
};
    // <NonSemanticallyEquivalentDirectories End>


    // <SemanticallyEquivalentDirectories Start>
   private static readonly string[] SemanticallyEquivalentDirectories = {
        "AbstractPublicConstructor",
        "AddBraces",
        "AddingReadonly",
        "AddParentheses",
        "AlwaysFalse",
        "AnonomousFunctionVsMethodGroup",
        "AnonymousType",
        "AssignSameVariable",
        "AttributesSeperated",
        "AutoImplementedProperty",
        "AvoidBoxing",
        "AvoidNestedTertiarys",
        "BlockBodyVsExpressionBody",
        "CantUseBatchFixAll",
        "Caret",
        "Coalesce",
        "CoalesceInIf",
        "Comments",
        "CompoundAssignment",
        "ConditionalAccess",
        "ConsistentVarVsType",
        "ConstantInAnonymous",
        "ConstantOnRHS",
        "ConstantOverField",
        "CountVsAny",
        "DebuggerDisplayAttribute",
        "DefaultLastInSwitch",
        "DefaultParam",
        "DoubleUsing",
        "DuplicateEnumVal",
        "ElementAccess",
        "EmptyDestructor",
        "EmptyElse",
        "EmptyFinally",
        "EmptyInitializer",
        "EmptyRegion",
        "EmptyStatement",
        "EmptyString",
        "EmptySwitch",
        "Enum",
        "EnumDefaultType",
        "EventArgsEmpty",
        "EventHandler",
        "ExceptionConstructors",
        "ExplicitEnum",
        "ExplicitEnumerator",
        "ExplicitlyTypedArray",
        "ExplicitObjectCreation",
        "ExtensionAsInstance",
        "ExtraCommaInitializer",
        "FileScopedNamespace",
        "FloatConst",
        "ForVsWhile",
        "HasFlag",
        "IfNesting",
        "IfToAssignment",
        "IfToReturn",
        "IncrementOperator",
        "InfiniteLoop",
        "Infinity",
        "InlineLocalVariable",
        "IsNullOrEmpty",
        "IsOperator",
        "JoinStringExpressions",
        "Label",
        "LambdaBodyVsExpressionBody",
        "LambdaVariableRename",
        "LambdaVsDelegate",
        "LazyInitialization",
        "ListPattern",
        "LocalAsConst",
        "LocalVariableOrder",
        "LocalVariableRename",
        "LogParams",
        "Maths",
        "MemberHidesInheritedMember",
        "MergePreprocessor",
        "MethodChaining",
        "NamedArgsOrder",
        "NameOf",
        "NestedIf",
        "NeverNullCheck",
        "NullableT",
        "NullCheck",
        "NullForgiving",
        "OperatorUnnecessary",
        "OptimizeLinq",
        "OptimizeMethodCall",
        "ParenthesesOnNewObject",
        "PatternMatch",
        "PolymorphicNullCoalesce",
        "PrivateVariableRename",
        "ReadonlyField",
        "Record",
        "RedundantAs",
        "RedundantAssignment",
        "RedundantAsyncAwait",
        "RedundantBaseConstructorCall",
        "RedundantBaseInterface",
        "RedundantBoolean",
        "RedundantBraces",
        "RedundantCast",
        "RedundantConstructor",
        "RedundantDispose",
        "RedundantFieldInitialization",
        "RedundantOverride",
        "RedundantStatement",
        "RedundantToCharArray",
        "RegexInstanceOverStatic",
        "RemoveArgumentListFromAttribute",
        "RemoveOriginalExceptionFromThrow",
        "RenameInFrom",
        "ReorderFieldsInClass",
        "ReorderStaticMethodsInClass",
        "ReorderUsings",
        "RepeatedLine",
        "SealedClass",
        "ShortCircuit",
        "SimplifyBranching",
        "SimplifyConditional",
        "SimplifyQualifiedName",
        "SimplityNestedUsing",
        "SortEnum",
        "SplitVarDecleration",
        "StaticMakesNoDifference",
        "StringBuilder",
        "StringBuilderPlusFormatting",
        "StringComparison",
        "StringEmptyCheck",
        "SupressNullableWarnings",
        "SwitchBraces",
        "SwitchSameContex",
        "ToStringUnneeded",
        "TrailingComma",
        "TupleInForeach",
        "TypeParameterConstraintOrder",
        "UnnecesaryInterpolation",
        "UnnecessaryAssignment",
        "UnnecessaryElse",
        "UnnecessaryInterpolation",
        "UnnecessaryNullCheck",
        "UnnecessaryUnsafe",
        "UnneededCase",
        "UnreachableCode",
        "UnusedPrivateMethods",
        "UnusedReturn",
        "UnusedUsing",
        "UnusedVariable",
        "UseConditionalAccess",
        "UsePredefinedType",
        "UsingAliasDirective",
        "UsingBracketed",
        "VariableOverlap",
        "VariableReuseInIfAndElse",
        "VerbatimString",
        "WhitespaceDifferences"
};
    // <SemanticallyEquivalentDirectories End>

    [Test]
    public void EnsureListOfSemanticallyEquivalentUpToDate()
    {
        var directory = new DirectoryInfo(SemanticEquivalentLocation);
        var shouldInclude = directory.GetDirectories()
            .Where(d => d.GetFiles().Any())
            .Select(d => d.Name)
            .OrderBy(x => x)
            .ToArray();

        if (!shouldInclude.SequenceEqual(SemanticallyEquivalentDirectories))
        {
            // Yeah this is a bit ugly once have the nicer interpolation from C# 11 will update
            var formattedDirectories = string.Join(",\n", shouldInclude.Select(s => $"        \"{s}\""));
            var expected = $"   private static readonly string[] SemanticallyEquivalentDirectories = {{\n{formattedDirectories}\n}};";
            UpdateListOfTests(expected, "<SemanticallyEquivalentDirectories Start>", "<SemanticallyEquivalentDirectories End>");
            Assert.Fail($"Replace SemanticallyEquivalentDirectories field with: \n{expected}");
        }
    }
    
    [Test]
    public void EnsureListOfNotSemanticallyEquivalentUptoDate()
    {
        var directory = new DirectoryInfo(NotSemanticallyEquivalentDirectory);
        var shouldInclude = directory.GetDirectories()
            .Where(d => d.GetFiles().Any())
            .Select(d => d.Name)
            .OrderBy(x => x)
            .ToArray();

        if (!shouldInclude.SequenceEqual(NonSemanticallyEquivalentDirectories))
        {
            // Yeah this is a bit ugly once have the nicer interpolation from C# 11 will update
            var formattedDirectories = string.Join(",\n", shouldInclude.Select(s => $"        \"{s}\""));
            var expected = $"   private static readonly string[] NonSemanticallyEquivalentDirectories = {{\n{formattedDirectories}\n}};";
            UpdateListOfTests(expected, "<NonSemanticallyEquivalentDirectories Start>", "<NonSemanticallyEquivalentDirectories End>");
            Assert.Fail($"Replace NonSemanticallyEquivalentDirectories field with: \n{expected}");
        }
    }
    
    private static void UpdateListOfTests(string updatedValue, string startTag, string endTag, [CallerFilePath] string? testFilePath=default)
    {
        var lines = File.ReadAllLines(testFilePath!).ToList();
        var start = lines.FindIndex(x => x.Contains(startTag));
        var end = lines.FindIndex(x => x.Contains(endTag));

        var result = String.Join("\n",lines.Take(start + 1))+$"\n{updatedValue}\n" + String.Join("\n",lines.Skip(end).ToList());
        File.WriteAllTextAsync(testFilePath!, result);
    }

    private static Solution BaseSolution { get; }
    private static ProjectId BaseProjectId { get; }
    private const string ProjectName = "NewProject";
    private static readonly string ProjectFilepath = Path.Join(Directory.GetCurrentDirectory(), ProjectName);

    private static (ProjectId, Solution) SetupProjectAndSolution()
    {        
        var workspace = new AdhocWorkspace();
        var dotnetAssembliesLocation  = Directory.GetParent(typeof(int).Assembly.Location)!.FullName;

        var dotnetLibs = Directory.GetFiles(dotnetAssembliesLocation)
            .Where(f => f.EndsWith(".dll")).OrderBy(x => x).ToList();
        var currentAssemblyLocation = Directory.GetParent(typeof(SemanticEquivalenceTests).Assembly.Location)!.FullName;

        var otherLibs = Directory.GetFiles(currentAssemblyLocation)
            .Where(f => f.EndsWith(".dll") && (f.Contains("xunit") || f.Contains("Microsoft")))
            .OrderBy(x => x)
            .ToList();
        
        var metadataRefs =  dotnetLibs.Concat(otherLibs).Select(f => MetadataReference.CreateFromFile(f));
        var projId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(projId, VersionStamp.Create(), ProjectName, ProjectName,
                LanguageNames.CSharp, metadataReferences: metadataRefs, filePath: ProjectFilepath,
                outputFilePath: Path.Join(Directory.GetCurrentDirectory(), "Out", ProjectName,projId.ToString()))
            .WithCompilationOptions(
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        var newProject = workspace.AddProject(projectInfo);
        // We want to be able to use these Utils without causing an error.
        workspace.AddDocument(GetDocumentInfo(".","BasicUtils.cs",newProject.Id));
        workspace.AddDocument(GetMainMethod(".", newProject.Id));
        return (newProject.Id, workspace.CurrentSolution);
    }

    static SemanticEquivalenceTests()
    {
        SemtexLog.InitializeLogging(LogLevel.Debug, false, "", false);

        // Using the fact that stuff is all immutable in Roslyn we can pay a one off setup cost for this workspace and utils.
        // This also improves the caching of compiles between multiple tests for 91 tests this results in a approx 50% reduction in test time.
        (BaseProjectId, BaseSolution) = SetupProjectAndSolution();
    }
    
    private static DocumentInfo GetMainMethod(string folder, ProjectId projectId)
    {
        const string text = """
                            [assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute(".NETCoreApp,Version=v7.0", FrameworkDisplayName = ".NET 7.0")]
                            public static class NoOpProgram{public static void Main(){}}// Needed to avoid CS5001
                            """;
        var source = SourceText.From(text);
        var id = DocumentId.CreateNewId(projectId);
        var loader = TextLoader.From(TextAndVersion.Create(source, VersionStamp.Create()));
        var name = "NoOpProgram.cs";
        var fp = Path.Combine(folder, name);
        return DocumentInfo.Create(id, name, loader: loader, filePath: fp);
    }

    [Test, TestCaseSource(nameof(SemanticallyEquivalentDirectories))]
    public async Task SemanticallyEquivalent(string folderName)
    {
        if (
            folderName == "DebuggerDisplayAttribute" // just too annoying
            || folderName == "ExceptionConstructors"
        )
        {
            return; // TODO clearly this is temporary
        }

        var folderPath = Path.Combine(SemanticEquivalentLocation, folderName);

        var (leftSimplified,rightSimplified) = await GetSimplifiedDocs(folderPath).ConfigureAwait(false);
        var result = await SemanticEqualBreakdown.GetSemanticallyUnequal(leftSimplified, rightSimplified).ConfigureAwait(false);
        
        
        var leftRaw = await leftSimplified.GetSyntaxRootAsync().ConfigureAwait(false);
        var rightRaw = await rightSimplified.GetSyntaxRootAsync().ConfigureAwait(false);

        var equal = result.Match(
            x => x.MethodIdentifiers.Count == 0,
            x => false
        );
        // this is slightly weird when all we care about is the result of match for test pass / fail but gives us a nicer output with the simplified form
        if (!equal)
        {
            leftRaw.Should().Be(rightRaw);
            Assert.Fail();
        }
        
        Console.WriteLine(leftRaw!.ToFullString());
        Console.WriteLine("**************************");
        Console.WriteLine(rightRaw!.ToFullString());
    }


    public static async Task<(Document, Document)> GetSimplifiedDocs(string folderPath)
    {
        var leftDocInfo = GetDocumentInfo(folderPath, "Left.cs", BaseProjectId);
        var leftSln = BaseSolution.AddDocument(leftDocInfo);
        var leftFp = new AbsolutePath(leftDocInfo.FilePath!);
        
        var rightDocInfo = GetDocumentInfo(folderPath, "Right.cs", BaseProjectId);
        var rightSln = BaseSolution.AddDocument(rightDocInfo);
        var rightFp = new AbsolutePath(rightDocInfo.FilePath!);

        leftSln =  await SimplifySolution(leftSln, leftFp).ConfigureAwait(false);
        rightSln =  await SimplifySolution(rightSln, rightFp).ConfigureAwait(false);
        
        var leftDoc = leftSln.Projects.SelectMany(p => p.Documents).First(d => d.FilePath == leftFp.Path);
        var rightDoc = rightSln.Projects.SelectMany(p => p.Documents).First(d => d.FilePath == rightFp.Path);
        
        (leftSln, rightSln) = await CheckSemanticEquivalence.CoSimplifySolutions(
            leftSln, 
            rightSln, 
            new HashSet<ProjectId>() { leftDoc.Project.Id }, 
            new HashSet<ProjectId>() { rightDoc.Project.Id }, 
            new []{(new AbsolutePath(leftDoc.FilePath!),new AbsolutePath(rightDoc.FilePath!))}
        );

        return (leftSln.GetDocument(leftDoc.Id)!, rightSln.GetDocument(rightDoc.Id)!);
    }

    [Test, TestCaseSource(nameof(NonSemanticallyEquivalentDirectories))]
    public async Task NotSemanticallyEquivalent(string folderName)
    {
        var folderPath = Path.Combine(NotSemanticallyEquivalentDirectory, folderName);

        var (leftSimplified,rightSimplified) = await GetSimplifiedDocs(folderPath).ConfigureAwait(false);
        var result = await SemanticEqualBreakdown.GetSemanticallyUnequal(leftSimplified, rightSimplified).ConfigureAwait(false);
        
        
        var leftRaw = await leftSimplified.GetSyntaxRootAsync().ConfigureAwait(false);
        var rightRaw = await rightSimplified.GetSyntaxRootAsync().ConfigureAwait(false);

        var equal = result.Match(
            x => x.MethodIdentifiers.Count == 0,
            x => false
        );
        // this is slightly weird when all we care about is the result of match for test pass / fail but gives us a nicer output with the simplified form
        if (equal)
        {
            Console.WriteLine(leftRaw!.ToFullString());
            Assert.Fail();
        }
        
        Console.WriteLine(leftRaw!.ToFullString());
        Console.WriteLine("**************************");
        Console.WriteLine(rightRaw!.ToFullString());

    }

    private static async Task<Solution> SimplifySolution(Solution sln, AbsolutePath fpToSimplify)
    {
        // Empty indicates just apply it to the whole solution
        var changeMethodsMap = new Dictionary<AbsolutePath, HashSet<MethodIdentifier>>();
        
        var projectIds = sln.Projects
            .Where(p => p.FilePath == ProjectFilepath)
            .Select(p => p.Id)
            .ToHashSet();

        return await SemanticSimplifier
            .GetSolutionWithFilesSimplified(sln, projectIds, new HashSet<AbsolutePath>(){fpToSimplify}, null, changeMethodsMap)
            .ConfigureAwait(false);
    }


    private static DocumentInfo GetDocumentInfo(string folder, string name, ProjectId projectId)
    {
        var filePath = Path.Combine(folder, name);
        var rawText = File.ReadAllText(filePath);
        var testText = StripOutLeftAndRight(rawText);
        var sourceLeft = SourceText.From(testText);
        var id = DocumentId.CreateNewId(projectId);
        var loader = TextLoader.From(TextAndVersion.Create(sourceLeft, VersionStamp.Create()));
        return DocumentInfo.Create(id, name, loader: loader, filePath: Path.GetFullPath(filePath));
    }

    private static string StripOutLeftAndRight(string text)
    {
        // This is pretty horrid.
        var patterns = new List<string>()
        {
            "public class <val>",
            "public partial class <val>",
            "public sealed class <val>",
            "public <val>",
            "enum <val>",
            "~<val>",
            "public record <val>",
            "private <val>",
            "interface <val>",
            "static <val>()"
        };

        foreach (var pattern in patterns)
        {
            var leftVersion = pattern.Replace("<val>", "Left");
            var rightVersion = pattern.Replace("<val>", "Right");
            var testVersion = pattern.Replace("<val>", "Test");
            text = text
                .Replace(leftVersion, testVersion)
                .Replace(rightVersion, testVersion);
        }

        return text;
    }
}