using CodingGame.Core.Rules;

namespace CodingGame.Core.Interpreter;

/// <summary>Finds the first use of a feature the player has not unlocked yet.</summary>
public static class UnlockCheck
{
    /// <summary>
    /// Walks a parsed script in source order and finds the first use of a locked feature.
    /// </summary>
    /// <param name="program">The parsed script.</param>
    /// <param name="lockedFeatures">The feature names that are still locked, as in data/unlocks.json, each with the unlock that gives it.</param>
    /// <returns>A <see cref="ScriptErrorKind.NotUnlocked"/> error that names the unlock, for the first locked use, or null when the script uses nothing locked. Null is the answer "no error", not a failure.</returns>
    public static ScriptError? FindLocked(ScriptProgram program, IReadOnlyDictionary<string, Unlock> lockedFeatures)
    {
        return FindInBlock(program.Body, program.Language, lockedFeatures, Declarations.Of(program));
    }

    private static ScriptError? FindInBlock(
        IReadOnlyList<Statement> statements,
        ScriptLanguage language,
        IReadOnlyDictionary<string, Unlock> lockedFeatures,
        DeclaredNames declared)
    {
        foreach (Statement statement in statements)
        {
            ScriptError? error = FindInStatement(statement, language, lockedFeatures, declared);
            if (error is not null)
            {
                return error;
            }
        }

        return null;
    }

    private static ScriptError? FindInStatement(
        Statement statement,
        ScriptLanguage language,
        IReadOnlyDictionary<string, Unlock> lockedFeatures,
        DeclaredNames declared)
    {
        return statement switch
        {
            ExpressionStatement expressionStatement =>
                FindInExpression(expressionStatement.Expression, language, lockedFeatures, declared),
            If conditional =>
                Check(conditional.Line, "if", "if", lockedFeatures)
                ?? FindInExpression(conditional.Condition, language, lockedFeatures, declared)
                ?? FindInBlock(conditional.Then, language, lockedFeatures, declared)
                ?? FindInBlock(conditional.Else, language, lockedFeatures, declared),
            While loop =>
                Check(loop.Line, "while", "while", lockedFeatures)
                ?? FindInExpression(loop.Condition, language, lockedFeatures, declared)
                ?? FindInBlock(loop.Body, language, lockedFeatures, declared),
            Repeat loop =>
                Check(loop.Line, "for", "for", lockedFeatures)
                ?? FindInExpression(loop.Count, language, lockedFeatures, declared)
                ?? FindInBlock(loop.Body, language, lockedFeatures, declared),
            Assign assignment =>
                Check(assignment.Line, "variables", "variables", lockedFeatures)
                ?? FindInExpression(assignment.Value, language, lockedFeatures, declared),
            FunctionDefinition definition =>
                Check(definition.Line, "def", FunctionKeyword(language), lockedFeatures)
                ?? FindInBlock(definition.Body, language, lockedFeatures, declared),
            _ => throw new InvalidOperationException("unknown statement type"),
        };
    }

    private static ScriptError? FindInExpression(
        Expression expression,
        ScriptLanguage language,
        IReadOnlyDictionary<string, Unlock> lockedFeatures,
        DeclaredNames declared)
    {
        return expression switch
        {
            Literal => null,
            Name name when IsVariableOrCounter(name.Identifier, declared) =>
                Check(name.Line, "variables", "variables", lockedFeatures),
            Name name => Check(name.Line, name.Identifier, name.Identifier, lockedFeatures),
            Call call =>
                Check(call.Line, call.Callee, call.Callee, lockedFeatures)
                ?? FindInArguments(call.Arguments, language, lockedFeatures, declared),
            Compare comparison =>
                Check(comparison.Line, ComparisonFeature(comparison.Operator), ComparisonText(comparison.Operator, language), lockedFeatures)
                ?? FindInExpression(comparison.Left, language, lockedFeatures, declared)
                ?? FindInExpression(comparison.Right, language, lockedFeatures, declared),
            Arithmetic arithmetic =>
                Check(arithmetic.Line, OperatorText.Of(arithmetic.Operator), OperatorText.Of(arithmetic.Operator), lockedFeatures)
                ?? FindInExpression(arithmetic.Left, language, lockedFeatures, declared)
                ?? FindInExpression(arithmetic.Right, language, lockedFeatures, declared),
            Negate negate =>
                Check(negate.Line, "-", "-", lockedFeatures)
                ?? FindInExpression(negate.Operand, language, lockedFeatures, declared),
            _ => throw new InvalidOperationException("unknown expression type"),
        };
    }

    private static ScriptError? FindInArguments(
        IReadOnlyList<Expression> arguments,
        ScriptLanguage language,
        IReadOnlyDictionary<string, Unlock> lockedFeatures,
        DeclaredNames declared)
    {
        foreach (Expression argument in arguments)
        {
            ScriptError? error = FindInExpression(argument, language, lockedFeatures, declared);
            if (error is not null)
            {
                return error;
            }
        }

        return null;
    }

    private static bool IsVariableOrCounter(string identifier, DeclaredNames declared)
    {
        return declared.Counters.Contains(identifier) || declared.Variables.Contains(identifier);
    }

    private static ScriptError? Check(
        int line,
        string feature,
        string detail,
        IReadOnlyDictionary<string, Unlock> lockedFeatures)
    {
        return lockedFeatures.TryGetValue(feature, out Unlock? unlock)
            ? new ScriptError(ScriptErrorKind.NotUnlocked, line, detail, unlock)
            : null;
    }

    private static string FunctionKeyword(ScriptLanguage language)
    {
        return language switch
        {
            ScriptLanguage.Python => "def",
            ScriptLanguage.JavaScript => "function",
            _ => throw new InvalidOperationException("unknown script language"),
        };
    }

    private static string ComparisonFeature(CompareOperator comparison)
    {
        return comparison is CompareOperator.Equal or CompareOperator.NotEqual ? "==" : OperatorText.Of(comparison);
    }

    private static string ComparisonText(CompareOperator comparison, ScriptLanguage language)
    {
        return (comparison, language) switch
        {
            (CompareOperator.Equal, ScriptLanguage.JavaScript) => "===",
            (CompareOperator.NotEqual, ScriptLanguage.JavaScript) => "!==",
            _ => OperatorText.Of(comparison),
        };
    }
}
