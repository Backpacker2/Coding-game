using System.Globalization;
using CodingGame.Core.Interpreter;

namespace CodingGame.Core.Tests.Interpreter.Python;

/// <summary>
/// Test-only text form of a program, so parser tests can compare strings instead of record trees.
/// </summary>
internal static class TreeDump
{
    /// <summary>
    /// Dumps a program with one line per statement and each block indented two spaces deeper.
    /// </summary>
    /// <param name="program">The program to dump.</param>
    /// <returns>The lines joined with newlines and no trailing newline. An empty program gives an empty string.</returns>
    /// <exception cref="InvalidOperationException">The program holds a literal that is neither a number nor a boolean, or an unknown node.</exception>
    internal static string Of(ScriptProgram program)
    {
        var lines = new List<string>();
        AddStatements(program.Body, 0, lines);
        return string.Join("\n", lines);
    }

    private static void AddStatements(IReadOnlyList<Statement> statements, int depth, List<string> lines)
    {
        string indent = new(' ', 2 * depth);
        foreach (Statement statement in statements)
        {
            switch (statement)
            {
                case ExpressionStatement expressionStatement:
                    lines.Add($"{indent}{expressionStatement.Line}: {FormatExpression(expressionStatement.Expression)}");
                    break;
                case If conditional:
                    lines.Add($"{indent}{conditional.Line}: if {FormatExpression(conditional.Condition)}");
                    AddStatements(conditional.Then, depth + 1, lines);
                    if (conditional.Else.Count > 0)
                    {
                        lines.Add($"{indent}else");
                        AddStatements(conditional.Else, depth + 1, lines);
                    }
                    break;
                case While loop:
                    lines.Add($"{indent}{loop.Line}: while {FormatExpression(loop.Condition)}");
                    AddStatements(loop.Body, depth + 1, lines);
                    break;
                case Repeat repeat:
                    lines.Add($"{indent}{repeat.Line}: repeat {FormatExpression(repeat.Count)}");
                    AddStatements(repeat.Body, depth + 1, lines);
                    break;
                case FunctionDefinition definition:
                    lines.Add($"{indent}{definition.Line}: def {definition.Name}");
                    AddStatements(definition.Body, depth + 1, lines);
                    break;
                default:
                    throw new InvalidOperationException($"unexpected statement {statement.GetType().Name}");
            }
        }
    }

    private static string FormatExpression(Expression expression)
    {
        return expression switch
        {
            Literal literal => FormatLiteral(literal.Value),
            Name name => name.Identifier,
            Call call => $"{call.Callee}({string.Join(", ", call.Arguments.Select(FormatExpression))})",
            Compare compare => $"({FormatExpression(compare.Left)} {FormatOperator(compare.Operator)} {FormatExpression(compare.Right)})",
            _ => throw new InvalidOperationException($"unexpected expression {expression.GetType().Name}"),
        };
    }

    private static string FormatLiteral(Value value)
    {
        return value.Kind switch
        {
            ValueKind.Number => value.AsNumber().ToString(CultureInfo.InvariantCulture),
            ValueKind.Boolean => value.AsBoolean() ? "true" : "false",
            _ => throw new InvalidOperationException($"unexpected literal kind {value.Kind}"),
        };
    }

    private static string FormatOperator(CompareOperator compareOperator)
    {
        return compareOperator switch
        {
            CompareOperator.Equal => "==",
            CompareOperator.NotEqual => "!=",
            _ => throw new InvalidOperationException($"unexpected operator {compareOperator}"),
        };
    }
}
