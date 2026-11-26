namespace CodingGame.Core.Interpreter;

/// <summary>Finds the first use of a feature the player has not unlocked yet.</summary>
public static class UnlockCheck
{
    /// <summary>
    /// Walks a parsed script in source order and finds the first use of a locked feature.
    /// </summary>
    /// <param name="program">The parsed script.</param>
    /// <param name="lockedFeatures">The feature names that are still locked, as in data/unlocks.json.</param>
    /// <returns>A <see cref="ScriptErrorKind.NotUnlocked"/> error for the first locked use, or null when the script uses nothing locked. Null is the answer "no error", not a failure.</returns>
    public static ScriptError? FindLocked(ScriptProgram program, IReadOnlySet<string> lockedFeatures)
    {
        return FindInBlock(program.Body, program.Language, lockedFeatures);
    }

    private static ScriptError? FindInBlock(
        IReadOnlyList<Statement> statements,
        ScriptLanguage language,
        IReadOnlySet<string> lockedFeatures)
    {
        foreach (Statement statement in statements)
        {
            ScriptError? error = FindInStatement(statement, language, lockedFeatures);
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
        IReadOnlySet<string> lockedFeatures)
    {
        return statement switch
        {
            ExpressionStatement expressionStatement =>
                FindInExpression(expressionStatement.Expression, language, lockedFeatures),
            If conditional =>
                Check(conditional.Line, "if", "if", lockedFeatures)
                ?? FindInExpression(conditional.Condition, language, lockedFeatures)
                ?? FindInBlock(conditional.Then, language, lockedFeatures)
                ?? FindInBlock(conditional.Else, language, lockedFeatures),
            While loop =>
                Check(loop.Line, "while", "while", lockedFeatures)
                ?? FindInExpression(loop.Condition, language, lockedFeatures)
                ?? FindInBlock(loop.Body, language, lockedFeatures),
            Repeat loop =>
                Check(loop.Line, "for", "for", lockedFeatures)
                ?? FindInExpression(loop.Count, language, lockedFeatures)
                ?? FindInBlock(loop.Body, language, lockedFeatures),
            FunctionDefinition definition =>
                Check(definition.Line, "def", FunctionKeyword(language), lockedFeatures)
                ?? FindInBlock(definition.Body, language, lockedFeatures),
            _ => throw new InvalidOperationException("unknown statement type"),
        };
    }

    private static ScriptError? FindInExpression(
        Expression expression,
        ScriptLanguage language,
        IReadOnlySet<string> lockedFeatures)
    {
        return expression switch
        {
            Literal => null,
            Name name => Check(name.Line, name.Identifier, name.Identifier, lockedFeatures),
            Call call =>
                Check(call.Line, call.Callee, call.Callee, lockedFeatures)
                ?? FindInArguments(call.Arguments, language, lockedFeatures),
            Compare comparison =>
                Check(comparison.Line, "==", ComparisonText(comparison.Operator, language), lockedFeatures)
                ?? FindInExpression(comparison.Left, language, lockedFeatures)
                ?? FindInExpression(comparison.Right, language, lockedFeatures),
            _ => throw new InvalidOperationException("unknown expression type"),
        };
    }

    private static ScriptError? FindInArguments(
        IReadOnlyList<Expression> arguments,
        ScriptLanguage language,
        IReadOnlySet<string> lockedFeatures)
    {
        foreach (Expression argument in arguments)
        {
            ScriptError? error = FindInExpression(argument, language, lockedFeatures);
            if (error is not null)
            {
                return error;
            }
        }

        return null;
    }

    private static ScriptError? Check(
        int line,
        string feature,
        string detail,
        IReadOnlySet<string> lockedFeatures)
    {
        return lockedFeatures.Contains(feature)
            ? new ScriptError(ScriptErrorKind.NotUnlocked, line, detail)
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

    private static string ComparisonText(CompareOperator comparison, ScriptLanguage language)
    {
        return (comparison, language) switch
        {
            (CompareOperator.Equal, ScriptLanguage.Python) => "==",
            (CompareOperator.Equal, ScriptLanguage.JavaScript) => "===",
            (CompareOperator.NotEqual, ScriptLanguage.Python) => "!=",
            (CompareOperator.NotEqual, ScriptLanguage.JavaScript) => "!==",
            _ => throw new InvalidOperationException("unknown comparison or script language"),
        };
    }
}
