namespace CodingGame.Core.Interpreter;

/// <summary>
/// An expression of a script. Every expression knows the line it starts on.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
public abstract record Expression(int Line);

/// <summary>
/// A value written directly in the script, such as a number.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
/// <param name="Value">The value of the literal.</param>
public sealed record Literal(int Line, Value Value) : Expression(Line);

/// <summary>
/// A name that refers to a constant of the drone API, such as NORTH.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
/// <param name="Identifier">The name as written in the script.</param>
public sealed record Name(int Line, string Identifier) : Expression(Line);

/// <summary>
/// A call of a command or a player function, with its arguments.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
/// <param name="Callee">The name of the command or function that is called.</param>
/// <param name="Arguments">The arguments, evaluated from left to right.</param>
public sealed record Call(int Line, string Callee, IReadOnlyList<Expression> Arguments) : Expression(Line);

/// <summary>
/// The comparison of two values.
/// </summary>
public enum CompareOperator
{
    /// <summary>The two values are equal.</summary>
    Equal,

    /// <summary>The two values are not equal.</summary>
    NotEqual,

    /// <summary>The left number is less than the right number.</summary>
    Less,

    /// <summary>The left number is greater than the right number.</summary>
    Greater,

    /// <summary>The left number is less than or equal to the right number.</summary>
    LessOrEqual,

    /// <summary>The left number is greater than or equal to the right number.</summary>
    GreaterOrEqual,
}

/// <summary>
/// Compares two values and gives a boolean.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
/// <param name="Left">The value on the left of the operator, evaluated first.</param>
/// <param name="Operator">The comparison to make.</param>
/// <param name="Right">The value on the right of the operator, evaluated second.</param>
public sealed record Compare(int Line, Expression Left, CompareOperator Operator, Expression Right) : Expression(Line);

/// <summary>
/// The arithmetic operators on whole numbers.
/// </summary>
public enum ArithmeticOperator
{
    /// <summary>Adds the two numbers.</summary>
    Add,

    /// <summary>Subtracts the right number from the left one.</summary>
    Subtract,

    /// <summary>Multiplies the two numbers.</summary>
    Multiply,
}

/// <summary>
/// Adds, subtracts or multiplies two numbers.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
/// <param name="Left">The number on the left of the operator, evaluated first.</param>
/// <param name="Operator">The operation to make.</param>
/// <param name="Right">The number on the right of the operator, evaluated second.</param>
public sealed record Arithmetic(int Line, Expression Left, ArithmeticOperator Operator, Expression Right) : Expression(Line);

/// <summary>
/// Negates a number, as in <c>-x</c>.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
/// <param name="Operand">The number that is negated.</param>
public sealed record Negate(int Line, Expression Operand) : Expression(Line);

/// <summary>
/// The logical operators that combine two conditions.
/// </summary>
public enum LogicalOperator
{
    /// <summary>Gives true when both conditions are true. The right one is skipped when the left one is false.</summary>
    And,

    /// <summary>Gives true when at least one condition is true. The right one is skipped when the left one is true.</summary>
    Or,
}

/// <summary>
/// Combines two conditions with <c>and</c> or <c>or</c>.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
/// <param name="Left">The condition on the left of the operator, evaluated first.</param>
/// <param name="Operator">The operator to apply.</param>
/// <param name="Right">The condition on the right of the operator, evaluated only when the left one does not decide the result.</param>
public sealed record Logical(int Line, Expression Left, LogicalOperator Operator, Expression Right) : Expression(Line);

/// <summary>
/// Negates a condition, as in <c>not x</c>.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
/// <param name="Operand">The condition that is negated.</param>
public sealed record Not(int Line, Expression Operand) : Expression(Line);

/// <summary>
/// The text of the operators as the player sees them in messages.
/// </summary>
internal static class OperatorText
{
    /// <summary>
    /// Gives the symbol of an arithmetic operator.
    /// </summary>
    /// <param name="arithmeticOperator">The operator.</param>
    /// <returns>The symbol, such as <c>+</c>.</returns>
    internal static string Of(ArithmeticOperator arithmeticOperator)
    {
        return arithmeticOperator switch
        {
            ArithmeticOperator.Add => "+",
            ArithmeticOperator.Subtract => "-",
            ArithmeticOperator.Multiply => "*",
            _ => throw new ArgumentOutOfRangeException(nameof(arithmeticOperator), arithmeticOperator, "unknown arithmetic operator"),
        };
    }

    /// <summary>
    /// Gives the symbol of an ordering comparison, or of <c>==</c> and <c>!=</c> in Python form.
    /// </summary>
    /// <param name="compareOperator">The comparison.</param>
    /// <returns>The symbol, such as <c>&lt;=</c>.</returns>
    internal static string Of(CompareOperator compareOperator)
    {
        return compareOperator switch
        {
            CompareOperator.Equal => "==",
            CompareOperator.NotEqual => "!=",
            CompareOperator.Less => "<",
            CompareOperator.Greater => ">",
            CompareOperator.LessOrEqual => "<=",
            CompareOperator.GreaterOrEqual => ">=",
            _ => throw new ArgumentOutOfRangeException(nameof(compareOperator), compareOperator, "unknown comparison"),
        };
    }

    /// <summary>
    /// Gives the word or the symbol of a logical operator in the language of the script.
    /// </summary>
    /// <param name="logicalOperator">The operator.</param>
    /// <param name="language">The language of the script.</param>
    /// <returns><c>and</c> or <c>or</c> in Python, <c>&amp;&amp;</c> or <c>||</c> in JavaScript.</returns>
    internal static string Of(LogicalOperator logicalOperator, ScriptLanguage language)
    {
        return (logicalOperator, language) switch
        {
            (LogicalOperator.And, ScriptLanguage.Python) => "and",
            (LogicalOperator.Or, ScriptLanguage.Python) => "or",
            (LogicalOperator.And, ScriptLanguage.JavaScript) => "&&",
            (LogicalOperator.Or, ScriptLanguage.JavaScript) => "||",
            _ => throw new ArgumentOutOfRangeException(nameof(logicalOperator), logicalOperator, "unknown logical operator"),
        };
    }

    /// <summary>
    /// Gives the word or the symbol of <c>not</c> in the language of the script.
    /// </summary>
    /// <param name="language">The language of the script.</param>
    /// <returns><c>not</c> in Python, <c>!</c> in JavaScript.</returns>
    internal static string OfNot(ScriptLanguage language)
    {
        return language switch
        {
            ScriptLanguage.Python => "not",
            ScriptLanguage.JavaScript => "!",
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, "unknown script language"),
        };
    }
}
