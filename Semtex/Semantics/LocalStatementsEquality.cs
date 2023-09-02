using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Semtex.Rewriters;

namespace Semtex.Semantics;

/// This is essentially implementing the following observation (Obsv1)
/// Assuming that S1, S2 are two pure statements 
/// AND S1.VariablesRead is distinct from S2.VariablesWrittenTo
/// AND vice versa.
/// => You can swap the order of S1 and S2 without changing the meaning of the program.
///
/// Here "Pure statement" is simplified to <see cref="LocalStatementsEquality.IsPureStatement"/>>
///
/// More specifically the steps are: 
/// 1. Split the methods into N blocks where each of the blocks is either 1 statement long or is N sequential pure statements.
/// 2. Foreach block:
/// 3. - Confirm the left and right block contain the same block else return false.
/// 4. - For each statement in the left block:
/// 5.   - Find the minimum number of pairwise swaps needed to reorder it the two statements so that they are in the same place.
/// 6.   - Confirm that Obsv1 holds for all these swaps.

public static class LocalStatementsEquality
{
    internal static bool SemanticallyEqualLocalStatements(SyntaxList<StatementSyntax> left,
        SyntaxList<StatementSyntax> right, SemanticModel leftSemanticModel, SemanticModel rightSemanticModel,
        List<(ISymbol left, string right)> proposedRenames)
    {
        var localVariableRenameRewriter = new LocalVariableRenameRewriter(proposedRenames, leftSemanticModel);

        if (left.Count != right.Count)
            return false;

        // 1. From description
        var simpleBlocks = GetSimpleBlockRanges(left, leftSemanticModel)
            .Zip(GetSimpleBlockRanges(right, rightSemanticModel));
        // 2. From description
        foreach (var (simpleBlockLeft, simpleBlockRight) in simpleBlocks)
        {
            if (simpleBlockLeft.Count != simpleBlockRight.Count)
                return false;
            // By doing the renaming at this point we avoids need for an extra compile, and avoids us having to manually
            // walk down the left and right syntax trees together more than once. However its quite possible that the
            // increase in complexity isn't worth it in which case we should just pull this out into its own pipeline step. That is done before the Semantic Equality.
            var renamedLeftSimpleBlock = proposedRenames.Count > 0
                ? simpleBlockLeft.Select(l => (StatementSyntax)localVariableRenameRewriter.Visit(l))
                : simpleBlockLeft;
            if (simpleBlockLeft.Count == 1)
            {
                if (renamedLeftSimpleBlock.Single().NormalizeWhitespace().ToString() != simpleBlockRight.Single().NormalizeWhitespace().ToString()) return false;
            }
            else
            {
                if (!IsReorderSemanticallyEquivalent(renamedLeftSimpleBlock.ToList(), simpleBlockRight)) return false;
            }
        }

        return true;
    }

    // Implements 1. from the description.
    private static IEnumerable<List<StatementSyntax>> GetSimpleBlockRanges(SyntaxList<StatementSyntax> statements, SemanticModel semanticModel)
    {
        var acc = new List<StatementSyntax>();
        foreach (var (statement, index) in statements.Select((x, i) => (x, i)))
        {
            if (IsPureStatement(statement, semanticModel))
            {
                acc.Add(statement);
            }
            else
            {
                if (acc.Count > 0) yield return acc;
                yield return new List<StatementSyntax>() { statement };
                acc = new List<StatementSyntax>();
            }
        }
        if (acc.Count > 0) yield return acc;
    }

    private static bool IsReorderSemanticallyEquivalent(List<StatementSyntax> simpleBlockLeft, List<StatementSyntax> simpleBlockRight)
    {
        var rightTexts = simpleBlockRight.Select(s => s.NormalizeWhitespace().ToString()).ToList();
        var leftTexts = simpleBlockLeft.Select(s => s.NormalizeWhitespace().ToString()).ToList();

        // 3. From description
        if (leftTexts.Any(s => !rightTexts.Contains(s)) || leftTexts.Count != rightTexts.Count)
            return false;

        if (leftTexts.Distinct().Count() < leftTexts.Count)
            return false; // This wouldn't be difficult to support but right now I don't think its worth it.

        // 4. From description
        foreach (var (leftText, leftIndex) in leftTexts.Select((t, i) => (t, i)))
        {
            foreach (var needSwapping in GetPairwiseSwapsRequired(leftTexts, rightTexts, leftIndex))
            {
                var needSwappingIndex = rightTexts.IndexOf(needSwapping);
                // We can swap two pure statements order if the values written to by the first statement are not read by the second statement and vice versa.
                if (SwapStatementsChangesSemantics(simpleBlockLeft[leftIndex], simpleBlockRight[needSwappingIndex]))
                    return false;
            }
        }

        return true;
    }

    // 5. From description
    private static IEnumerable<string> GetPairwiseSwapsRequired(List<string> leftStatements,
        List<string> rightStatements, int leftIndex)
    {
        var rightIndex = rightStatements.IndexOf(leftStatements[leftIndex]);
        var beforeLeft = leftStatements.Take(leftIndex);
        var afterRight = rightStatements.Skip(rightIndex + 1);
        return beforeLeft.Intersect(afterRight);
    }

    // 6. From description
    private static bool SwapStatementsChangesSemantics(StatementSyntax left, StatementSyntax right)
    {
        var (leftRead, leftWrite) = GetReadsAndWrites(left);
        var (rightRead, rightWrite) = GetReadsAndWrites(right);
        return leftRead.Intersect(rightWrite).Any() || leftWrite.Intersect(rightRead).Any();

    }


    // Implements semantic approximation to pure check (100% Precision, < 100% Recall) 
    private static bool IsPureStatement(StatementSyntax statement, SemanticModel semanticModel)
    {
        switch (statement)
        {
            case LocalDeclarationStatementSyntax localDeclarationStatementSyntax:
                if (localDeclarationStatementSyntax.AttributeLists.Any() ||
                    localDeclarationStatementSyntax.ContainsDirectives ||
                    localDeclarationStatementSyntax.ContainsAnnotations
                   )
                    return false;
                // Do not care about either the type or the variable name at this point
                // TODO think about the multi variable case.
                if (localDeclarationStatementSyntax.Declaration.Variables.Count > 1)
                {
                    return false;
                }

                // do we need to think about semantic model here?
                var variable = localDeclarationStatementSyntax.Declaration.Variables.First();

                if (variable.Initializer == null)
                    return true;

                return IsSimpleExpression(variable.Initializer.Value, semanticModel);
            case ExpressionStatementSyntax expressionStatementSyntax:
                if (expressionStatementSyntax.AttributeLists.Any() ||
                    expressionStatementSyntax.ContainsDirectives ||
                    expressionStatementSyntax.ContainsAnnotations)
                    return false;

                return IsSimpleExpression(expressionStatementSyntax.Expression, semanticModel);
            default:
                return false;
        }
    }

    private static bool IsSimpleExpression(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        // For now we want to keep the set of things in here to be simple expression.
        switch (expression)
        {
            case BinaryExpressionSyntax binaryExpression:
                var symbol = semanticModel.GetSymbolInfo(binaryExpression).Symbol;
                // if the operator is overriden then this could contain arbitrary code.
                if (symbol is not IMethodSymbol { MethodKind: MethodKind.BuiltinOperator })
                    return false;

                return IsSimpleExpression(binaryExpression.Left, semanticModel) &&
                       IsSimpleExpression(binaryExpression.Right, semanticModel);
            case IdentifierNameSyntax identifierNameSyntax:
                return true;
            case LiteralExpressionSyntax literalExpressionSyntax:
                return true;
            case AssignmentExpressionSyntax assignmentExpressionSyntax:
                return assignmentExpressionSyntax.Left is IdentifierNameSyntax &&
                       IsSimpleExpression(assignmentExpressionSyntax.Right, semanticModel);

            default:
                return false;
        }
    }

    private static (IEnumerable<string> reads, IEnumerable<string> writes) GetReadsAndWrites(StatementSyntax statementSyntax)
    {
        switch (statementSyntax)
        {
            case LocalDeclarationStatementSyntax localDeclarationStatementSyntax:
                var variable = localDeclarationStatementSyntax.Declaration.Variables.First();
                var writes = new List<string>() { variable.Identifier.Text };
                if (variable.Initializer == null)
                    return (new List<string>(), writes);

                var initializerResults = GetReadsAndWrites(variable.Initializer.Value);
                return (initializerResults.reads, initializerResults.writes.Concat(writes));
            case ExpressionStatementSyntax expressionStatementSyntax:
                return GetReadsAndWrites(expressionStatementSyntax.Expression);
            default:
                throw new InvalidOperationException($"Unexpected Statement with kind {statementSyntax.Kind()}");
        }
    }
    private static (IEnumerable<string> reads, IEnumerable<string> writes) GetReadsAndWrites(ExpressionSyntax statementSyntax)
    {
        switch (statementSyntax)
        {
            case AssignmentExpressionSyntax expressionStatementSyntax:
                if (expressionStatementSyntax.Left is not IdentifierNameSyntax leftIdentifierNameSyntax)
                    throw new InvalidOperationException("Only support for assignment to IdentifierNames.");

                var rhs = GetReadsAndWrites(expressionStatementSyntax.Right);
                return (rhs.reads, rhs.writes.Append(leftIdentifierNameSyntax.Identifier.Text));
            case BinaryExpressionSyntax binaryExpression:
                var leftResults = GetReadsAndWrites(binaryExpression.Left);
                var rightResults = GetReadsAndWrites(binaryExpression.Right);
                return (leftResults.reads.Concat(rightResults.reads), leftResults.writes.Concat(rightResults.writes));
            case IdentifierNameSyntax identifierNameSyntax:
                return (new List<string>() { identifierNameSyntax.Identifier.Text }, new List<string>());
            case LiteralExpressionSyntax:
                return (new List<string>(), new List<string>());
            default:
                throw new InvalidOperationException($"Unexpected Statement with kind {statementSyntax.Kind()}");
        }
    }

}