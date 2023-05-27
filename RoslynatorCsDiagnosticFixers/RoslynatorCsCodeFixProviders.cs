using Microsoft.CodeAnalysis.CodeFixes;
using Roslynator.CSharp.CodeFixes;

namespace RoslynatorCsDiagnosticFixers;

public class RoslynatorCsCodeFixProviders
{
    public static Dictionary<string, CodeFixProvider> SupportedCodeFixes =
        new Dictionary<string, CodeFixProvider>()
        {
            ["CS0162"] = new UnreachableCodeCodeFixProvider(),
            ["CS0109"] = new RemoveNewModifierCodeFixProvider(),
            ["CS0164"] = new LabeledStatementCodeFixProvider(), // Note this is fixed in my fork but not yet on main fork
            ["CS0472"] = new ExpressionCodeFixProvider(),
            ["CS1522"] = new StatementCodeFixProvider(),
            ["CS1717"] = new AssignmentExpressionCodeFixProvider(),
        };
}