namespace CodingGame.Core.Interpreter;

/// <summary>
/// A statement of a script. Every statement knows the line it starts on.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
public abstract record Statement(int Line);

/// <summary>
/// A statement that is an expression, such as a command call.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
/// <param name="Expression">The expression that is evaluated. Its value is discarded.</param>
public sealed record ExpressionStatement(int Line, Expression Expression) : Statement(Line);

/// <summary>
/// Runs one block when a condition is true and another when it is false.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
/// <param name="Condition">The condition, which must be a boolean.</param>
/// <param name="Then">The statements that run when the condition is true.</param>
/// <param name="Else">The statements that run when the condition is false. Empty when there is no else. An elif is an <see cref="If"/> as the only statement here.</param>
public sealed record If(
    int Line,
    Expression Condition,
    IReadOnlyList<Statement> Then,
    IReadOnlyList<Statement> Else) : Statement(Line);

/// <summary>
/// Runs a block again as long as a condition is true.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
/// <param name="Condition">The condition, checked before each iteration. It must be a boolean.</param>
/// <param name="Body">The statements of one iteration.</param>
public sealed record While(int Line, Expression Condition, IReadOnlyList<Statement> Body) : Statement(Line);

/// <summary>
/// Runs a block a fixed number of times.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
/// <param name="Count">The number of iterations. It must be a number.</param>
/// <param name="Body">The statements of one iteration.</param>
/// <param name="Counter">The name the loop counter is read by inside the body, or null when the loop has no counter. The counter holds 0, 1, ... and is read-only.</param>
public sealed record Repeat(int Line, Expression Count, IReadOnlyList<Statement> Body, string? Counter) : Statement(Line);

/// <summary>
/// Gives a variable a new value. A variable is created by its first assignment.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
/// <param name="Name">The name of the variable.</param>
/// <param name="Value">The expression whose value is stored. It is evaluated before the variable changes.</param>
public sealed record Assign(int Line, string Name, Expression Value) : Statement(Line);

/// <summary>
/// Defines a player function. Running the definition registers the function under its name.
/// </summary>
/// <param name="Line">The line in the script, counted from 1.</param>
/// <param name="Name">The name the function is called by.</param>
/// <param name="Body">The statements the function runs when it is called.</param>
public sealed record FunctionDefinition(int Line, string Name, IReadOnlyList<Statement> Body) : Statement(Line);
