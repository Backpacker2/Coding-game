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
                if (_api.Constants.TryGetValue(name.Identifier, out value)
                    || TryCounterValue(name.Identifier, out value)
                    || _variables.TryGetValue(name.Identifier, out value))
                {
                    return true;
                }

                _failure = new ScriptError(ScriptErrorKind.UndefinedName, name.Line, name.Identifier);
                return false;
            case Call call:
                return TryCall(call, out value);
            case Compare compare:
                return TryCompare(compare, out value);
            case Arithmetic arithmetic:
                return TryArithmetic(arithmetic, out value);
            case Negate negate:
                return TryNegate(negate, out value);
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

    // The innermost loop that has this counter is the one in scope: the parser rejects a counter
    // that is read outside its loop, and a nested loop with the same counter name.
    private bool TryCounterValue(string name, out Value value)
    {
        foreach (Frame frame in _frames)
        {
            if (frame.Loop is Repeat { Counter: { } counter } && counter == name)
            {
                value = Value.FromNumber(frame.Iteration);
                return true;
            }
        }

        value = Value.None;
        return false;
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

        if (compare.Operator is CompareOperator.Equal or CompareOperator.NotEqual)
        {
            bool equal = left == right;
            result = Value.FromBoolean(compare.Operator == CompareOperator.Equal ? equal : !equal);
            return true;
        }

        if (!RequireNumbers(compare.Line, OperatorText.Of(compare.Operator), left, right))
        {
            return false;
        }

        int leftNumber = left.AsNumber();
        int rightNumber = right.AsNumber();
        result = Value.FromBoolean(compare.Operator switch
        {
            CompareOperator.Less => leftNumber < rightNumber,
            CompareOperator.Greater => leftNumber > rightNumber,
            CompareOperator.LessOrEqual => leftNumber <= rightNumber,
            CompareOperator.GreaterOrEqual => leftNumber >= rightNumber,
            _ => throw new ArgumentOutOfRangeException(nameof(compare), compare.Operator, "unknown comparison"),
        });
        return true;
    }

    private bool TryArithmetic(Arithmetic arithmetic, out Value result)
    {
        result = Value.None;
        if (!TryEvaluate(arithmetic.Left, out Value left))
        {
            return false;
        }

        if (!TryEvaluate(arithmetic.Right, out Value right))
        {
            return false;
        }

        if (!RequireNumbers(arithmetic.Line, OperatorText.Of(arithmetic.Operator), left, right))
        {
            return false;
        }

        // The operands are int, so a long holds the exact result of any of the three operations.
        long number = arithmetic.Operator switch
        {
            ArithmeticOperator.Add => (long)left.AsNumber() + right.AsNumber(),
            ArithmeticOperator.Subtract => (long)left.AsNumber() - right.AsNumber(),
            ArithmeticOperator.Multiply => (long)left.AsNumber() * right.AsNumber(),
            _ => throw new ArgumentOutOfRangeException(nameof(arithmetic), arithmetic.Operator, "unknown arithmetic operator"),
        };
        return TryNumberResult(arithmetic.Line, number, out result);
    }

    private bool TryNegate(Negate negate, out Value result)
    {
        result = Value.None;
        if (!TryEvaluate(negate.Operand, out Value operand))
        {
            return false;
        }

        if (operand.Kind != ValueKind.Number)
        {
            _failure = new ScriptError(
                ScriptErrorKind.WrongArgument,
                negate.Line,
                $"- needs a number, got {DescribeKind(operand.Kind)}");
            return false;
        }

        return TryNumberResult(negate.Line, -(long)operand.AsNumber(), out result);
    }

    private bool RequireNumbers(int line, string operatorText, Value left, Value right)
    {
        if (left.Kind == ValueKind.Number && right.Kind == ValueKind.Number)
        {
            return true;
        }

        _failure = new ScriptError(
            ScriptErrorKind.WrongArgument,
            line,
            $"{operatorText} needs two numbers, got {DescribeKind(left.Kind)} and {DescribeKind(right.Kind)}");
        return false;
    }

    private bool TryNumberResult(int line, long number, out Value result)
    {
        if (number is < int.MinValue or > int.MaxValue)
        {
            _failure = new ScriptError(ScriptErrorKind.WrongArgument, line, "number too big for this game");
            result = Value.None;
            return false;
        }

        result = Value.FromNumber((int)number);
        return true;
    }

    private static string DescribeKind(ValueKind kind)
    {
        return kind == ValueKind.None ? "nothing" : $"a {KindName(kind)}";
    }

    private static string KindName(ValueKind kind)
    {
        return kind.ToString().ToLowerInvariant();
    }
}
