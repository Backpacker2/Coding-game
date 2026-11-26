using CodingGame.Core.Rules;

namespace CodingGame.Core.Interpreter;

/// <summary>
/// Checks the names of a parsed script before it runs: where loop counters may be read, which names
/// a variable or counter may take, and where a variable may be changed.
/// </summary>
public static class ScopeCheck
{
    /// <summary>
    /// Finds the first name error in a parsed script, in source order.
    /// </summary>
    /// <param name="program">The parsed script.</param>
    /// <param name="api">The drone API whose commands and constants are game names that a variable or counter may not take.</param>
    /// <returns>The first error, or null when every name is used correctly. Null is the answer "no error", not a failure.</returns>
    public static ScriptError? Find(ScriptProgram program, DroneApi api)
    {
        var checker = new Checker(Declarations.Of(program), api);
        return checker.FindInBlock(program.Body, inFunction: false);
    }

    private sealed class Checker(DeclaredNames declared, DroneApi api)
    {
        private readonly List<string> _enclosingCounters = [];

        public ScriptError? FindInBlock(IReadOnlyList<Statement> statements, bool inFunction)
        {
            foreach (Statement statement in statements)
            {
                ScriptError? error = FindInStatement(statement, inFunction);
                if (error is not null)
                {
                    return error;
                }
            }

            return null;
        }

        private ScriptError? FindInStatement(Statement statement, bool inFunction)
        {
            return statement switch
            {
                ExpressionStatement expressionStatement => FindInExpression(expressionStatement.Expression),
                If conditional =>
                    FindInExpression(conditional.Condition)
                    ?? FindInBlock(conditional.Then, inFunction)
                    ?? FindInBlock(conditional.Else, inFunction),
                While loop => FindInExpression(loop.Condition) ?? FindInBlock(loop.Body, inFunction),
                Repeat loop => FindInExpression(loop.Count) ?? FindInRepeatBody(loop, inFunction),
                Assign assignment =>
                    CheckVariableName(assignment.Line, assignment.Name)
                    ?? (inFunction ? NotInFunction(assignment.Line) : null)
                    ?? FindInExpression(assignment.Value),
                FunctionDefinition definition => FindInBlock(definition.Body, inFunction: true),
                _ => throw new InvalidOperationException("unknown statement type"),
            };
        }

        private ScriptError? FindInRepeatBody(Repeat loop, bool inFunction)
        {
            if (loop.Counter is not { } counter)
            {
                return FindInBlock(loop.Body, inFunction);
            }

            ScriptError? error = CheckCounterName(loop.Line, counter);
            if (error is not null)
            {
                return error;
            }

            _enclosingCounters.Add(counter);
            error = FindInBlock(loop.Body, inFunction);
            _enclosingCounters.RemoveAt(_enclosingCounters.Count - 1);
            return error;
        }

        private ScriptError? FindInExpression(Expression expression)
        {
            return expression switch
            {
                Literal => null,
                Name name => CheckNameRead(name),
                Call call => FindInArguments(call.Arguments),
                Compare comparison => FindInExpression(comparison.Left) ?? FindInExpression(comparison.Right),
                Arithmetic arithmetic => FindInExpression(arithmetic.Left) ?? FindInExpression(arithmetic.Right),
                Negate negate => FindInExpression(negate.Operand),
                _ => throw new InvalidOperationException("unknown expression type"),
            };
        }

        private ScriptError? FindInArguments(IReadOnlyList<Expression> arguments)
        {
            foreach (Expression argument in arguments)
            {
                ScriptError? error = FindInExpression(argument);
                if (error is not null)
                {
                    return error;
                }
            }

            return null;
        }

        private ScriptError? CheckNameRead(Name name)
        {
            if (declared.Counters.Contains(name.Identifier) && !_enclosingCounters.Contains(name.Identifier))
            {
                return new ScriptError(
                    ScriptErrorKind.NameOutOfScope,
                    name.Line,
                    $"'{name.Identifier}' can only be used inside its for loop");
            }

            return null;
        }

        private ScriptError? CheckCounterName(int line, string name)
        {
            if (IsGameName(name))
            {
                return GameNameError(line, name);
            }

            if (_enclosingCounters.Contains(name))
            {
                return Syntax(line, $"'{name}' is already the counter of an outer loop; pick another name");
            }

            if (declared.Functions.Contains(name))
            {
                return Syntax(line, $"'{name}' is already a function; pick another name");
            }

            return null;
        }

        private ScriptError? CheckVariableName(int line, string name)
        {
            if (IsGameName(name))
            {
                return GameNameError(line, name);
            }

            if (_enclosingCounters.Contains(name))
            {
                return Syntax(line, $"'{name}' is a loop counter and cannot be changed");
            }

            if (declared.Counters.Contains(name))
            {
                return Syntax(line, $"'{name}' is already a loop counter; pick another name");
            }

            if (declared.Functions.Contains(name))
            {
                return Syntax(line, $"'{name}' is already a function; pick another name");
            }

            return null;
        }

        // range is the name the Python for loop uses. JavaScript has no such name, but both
        // languages share these rules, so a variable called range is refused in both.
        private bool IsGameName(string name)
        {
            return api.Commands.ContainsKey(name) || api.Constants.ContainsKey(name) || name == "range";
        }

        private static ScriptError GameNameError(int line, string name)
        {
            return Syntax(line, $"'{name}' is a name the game uses; pick another name");
        }

        private static ScriptError NotInFunction(int line)
        {
            return new ScriptError(ScriptErrorKind.NotInGame, line, "changing a variable inside a function");
        }

        private static ScriptError Syntax(int line, string detail)
        {
            return new ScriptError(ScriptErrorKind.SyntaxError, line, detail);
        }
    }
}
