namespace CodingGame.Core.Interpreter;

public sealed partial class Evaluator
{
    private bool TryEvaluate(Expression expression, out Value value)
    {
        switch (expression)
        {
            case Literal literal:
                value = literal.Value;
                return true;
            case Name name:
                if (_api.Constants.TryGetValue(name.Identifier, out value))
                {
                    return true;
                }

                _failure = new ScriptError(ScriptErrorKind.UndefinedName, name.Line, name.Identifier);
                return false;
            case Call call:
                return TryCall(call, out value);
            case Compare compare:
                return TryCompare(compare, out value);
            default:
                throw new InvalidOperationException($"unknown expression type {expression.GetType().Name}");
        }
    }

    private bool TryCall(Call call, out Value result)
    {
        result = Value.None;
        if (_functions.ContainsKey(call.Callee))
        {
            _failure = new ScriptError(
                ScriptErrorKind.WrongArgument,
                call.Line,
                $"{call.Callee} returns nothing and cannot be used in an expression");
            return false;
        }

        if (!_api.Commands.TryGetValue(call.Callee, out DroneCommand? command))
        {
            _failure = new ScriptError(ScriptErrorKind.UndefinedName, call.Line, call.Callee);
            return false;
        }

        var arguments = new Value[call.Arguments.Count];
        for (int index = 0; index < arguments.Length; index++)
        {
            if (!TryEvaluate(call.Arguments[index], out arguments[index]))
            {
                return false;
            }
        }

        if (arguments.Length != command.ArgumentKinds.Count)
        {
            _failure = new ScriptError(
                ScriptErrorKind.WrongArgument,
                call.Line,
                $"{call.Callee} expects {command.ArgumentKinds.Count} argument(s), got {arguments.Length}");
            return false;
        }

        for (int index = 0; index < arguments.Length; index++)
        {
            ValueKind expected = command.ArgumentKinds[index];
            ValueKind actual = arguments[index].Kind;
            if (actual != expected)
            {
                _failure = new ScriptError(
                    ScriptErrorKind.WrongArgument,
                    call.Line,
                    $"{call.Callee} expects {KindName(expected)}, got {KindName(actual)}");
                return false;
            }
        }

        CommandOutcome outcome = command.Run(arguments);
        if (outcome.FailureDetail is { } detail)
        {
            _failure = new ScriptError(ScriptErrorKind.CommandFailed, call.Line, detail);
            return false;
        }

        _pendingTicks += command.CostTicks;
        result = outcome.Result;
        return true;
    }

    private bool TryCompare(Compare compare, out Value result)
    {
        result = Value.None;
        if (!TryEvaluate(compare.Left, out Value left))
        {
            return false;
        }

        if (!TryEvaluate(compare.Right, out Value right))
        {
            return false;
        }

        bool equal = left == right;
        result = Value.FromBoolean(compare.Operator switch
        {
            CompareOperator.Equal => equal,
            CompareOperator.NotEqual => !equal,
            _ => throw new ArgumentOutOfRangeException(nameof(compare), compare.Operator, "unknown comparison"),
        });
        return true;
    }

    private static string KindName(ValueKind kind)
    {
        return kind.ToString().ToLowerInvariant();
    }
}
