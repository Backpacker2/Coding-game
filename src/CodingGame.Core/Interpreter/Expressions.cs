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
}

/// <summary>
/// Compares two values and gives a boolean.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
/// <param name="Left">The value on the left of the operator, evaluated first.</param>
/// <param name="Operator">The comparison to make.</param>
/// <param name="Right">The value on the right of the operator, evaluated second.</param>
public sealed record Compare(int Line, Expression Left, CompareOperator Operator, Expression Right) : Expression(Line);
