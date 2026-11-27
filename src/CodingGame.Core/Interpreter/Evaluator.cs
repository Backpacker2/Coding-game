namespace CodingGame.Core.Interpreter;

/// <summary>
/// Runs a script one simulation tick at a time against the drone API. Each command takes effect when
/// it is reached, then the field advances by its cost in ticks, one tick per step that returns running.
/// </summary>
public sealed partial class Evaluator
{
    // A runaway recursion must end as a script error long before it exhausts memory, and 100 is deep
    // enough for any lesson script.
    /// <summary>
    /// The number of player function calls that may be active at once. A call beyond it is a script error.
    /// </summary>
    public const int MaxCallDepth = 100;

    private readonly DroneApi _api;
    private readonly ScriptLanguage _language;
    private readonly Stack<Frame> _frames = new();
    private readonly Dictionary<string, FunctionDefinition> _functions = new();
    private readonly Dictionary<string, Value> _variables = new();
    private int _callDepth;
    private int _pendingTicks;
    private int _waitTicks;
    private StepResult? _final;
    private ScriptError? _failure;

    /// <summary>
    /// Creates an evaluator that runs a script from its first statement.
    /// </summary>
    /// <param name="program">The script to run.</param>
    /// <param name="api">The drone API the script calls. Its field is advanced as the script runs.</param>
    public Evaluator(ScriptProgram program, DroneApi api)
    {
        _api = api;
        _language = program.Language;
        _frames.Push(new Frame(program.Body));
    }

    /// <summary>
    /// Gets the line of the statement or loop iteration that was started last. It is 0 before the first step.
    /// </summary>
    public int CurrentLine { get; private set; }

    /// <summary>
    /// Gets the number of steps that returned running. It is the number of ticks the script has used.
    /// </summary>
    public int TicksUsed { get; private set; }

    /// <summary>
    /// Runs the script until its next tick is used, or until it finishes or fails. Once the script has
    /// finished or failed, every later step returns the same result and advances nothing.
    /// </summary>
    /// <returns>Running after a tick, Finished when the script ran to its end, or Failed with the script error.</returns>
    public StepResult Step()
    {
        if (_final is { } final)
        {
            return final;
        }

        if (_waitTicks > 0)
        {
            _waitTicks--;
            SpendTick();
            return StepResult.Running;
        }

        while (true)
        {
            if (_frames.Count == 0)
            {
                _final = StepResult.Finished;
                return _final.Value;
            }

            Frame frame = _frames.Peek();
            if (frame.Index == frame.Statements.Count)
            {
                EndFrame(frame);
            }
            else
            {
                Statement statement = frame.Statements[frame.Index];
                frame.Index++;
                CurrentLine = statement.Line;
                Execute(statement);
            }

            if (_failure is { } failure)
            {
                _final = StepResult.Failed(failure);
                return _final.Value;
            }

            if (ChargePendingTicks())
            {
                return StepResult.Running;
            }
        }
    }

    private void Execute(Statement statement)
    {
        switch (statement)
        {
            case ExpressionStatement { Expression: Call call } when _functions.ContainsKey(call.Callee):
                CallFunction(call);
                break;
            case ExpressionStatement expressionStatement:
                TryEvaluate(expressionStatement.Expression, out _);
                break;
            case If conditional:
                ExecuteIf(conditional);
                break;
            case While loop:
                ExecuteWhile(loop);
                break;
            case Repeat repeat:
                ExecuteRepeat(repeat);
                break;
            case FunctionDefinition definition:
                _functions[definition.Name] = definition;
                break;
            case Assign assignment:
                ExecuteAssign(assignment);
                break;
            default:
                throw new InvalidOperationException($"unknown statement type {statement.GetType().Name}");
        }
    }

    private void CallFunction(Call call)
    {
        if (call.Arguments.Count != 0)
        {
            _failure = new ScriptError(
                ScriptErrorKind.WrongArgument,
                call.Line,
                $"{call.Callee} expects 0 argument(s), got {call.Arguments.Count}");
            return;
        }

        if (_callDepth == MaxCallDepth)
        {
            _failure = new ScriptError(ScriptErrorKind.TooDeep, call.Line, call.Callee);
            return;
        }

        _callDepth++;
        _frames.Push(new Frame(_functions[call.Callee].Body) { IsFunctionCall = true });
        _pendingTicks += _api.FunctionCallTicks;
    }

    private void ExecuteIf(If conditional)
    {
        if (TryCondition(conditional.Condition, out bool holds))
        {
            _frames.Push(new Frame(holds ? conditional.Then : conditional.Else));
        }
    }

    private void ExecuteWhile(While loop)
    {
        if (TryCondition(loop.Condition, out bool holds) && holds)
        {
            _frames.Push(new Frame(loop.Body) { Loop = loop });
            _pendingTicks += _api.LoopIterationTicks;
        }
    }

    private void ExecuteAssign(Assign assignment)
    {
        if (TryEvaluate(assignment.Value, out Value value))
        {
            _variables[assignment.Name] = value;
            _pendingTicks += _api.AssignTicks;
        }
    }

    private void ExecuteRepeat(Repeat repeat)
    {
        if (!TryEvaluate(repeat.Count, out Value count))
        {
            return;
        }

        if (count.Kind != ValueKind.Number)
        {
            _failure = new ScriptError(
                ScriptErrorKind.WrongArgument,
                repeat.Count.Line,
                $"repeat count must be a number, got {KindName(count.Kind)}");
            return;
        }

        int iterations = count.AsNumber();
        if (iterations > 0)
        {
            _frames.Push(new Frame(repeat.Body) { Loop = repeat, RepeatsLeft = iterations - 1 });
            _pendingTicks += _api.LoopIterationTicks;
        }
    }

    private void EndFrame(Frame frame)
    {
        switch (frame.Loop)
        {
            case Repeat repeat when frame.RepeatsLeft > 0:
                frame.RepeatsLeft--;
                frame.Iteration++;
                frame.Index = 0;
                CurrentLine = repeat.Line;
                _pendingTicks += _api.LoopIterationTicks;
                break;
            case While loop:
                CurrentLine = loop.Line;
                if (TryCondition(loop.Condition, out bool holds) && holds)
                {
                    frame.Index = 0;
                    _pendingTicks += _api.LoopIterationTicks;
                }
                else
                {
                    _frames.Pop();
                }
                break;
            default:
                _frames.Pop();
                if (frame.IsFunctionCall)
                {
                    _callDepth--;
                }
                break;
        }
    }

    private bool TryCondition(Expression condition, out bool holds)
    {
        holds = false;
        if (!TryEvaluate(condition, out Value value))
        {
            return false;
        }

        if (value.Kind != ValueKind.Boolean)
        {
            _failure = new ScriptError(
                ScriptErrorKind.WrongArgument,
                condition.Line,
                $"condition must be true or false, got {KindName(value.Kind)}");
            return false;
        }

        holds = value.AsBoolean();
        return true;
    }

    // Charges the ticks that were added since the last charge. The first tick is spent now and the rest
    // are spent by the following steps, so the script waits for them before it continues.
    private bool ChargePendingTicks()
    {
        if (_pendingTicks == 0)
        {
            return false;
        }

        _waitTicks = _pendingTicks - 1;
        _pendingTicks = 0;
        SpendTick();
        return true;
    }

    private void SpendTick()
    {
        TicksUsed++;
        _api.Field.Advance(1);
    }

    private sealed class Frame(IReadOnlyList<Statement> statements)
    {
        public IReadOnlyList<Statement> Statements { get; } = statements;

        public int Index { get; set; }

        public Statement? Loop { get; init; }

        public int RepeatsLeft { get; set; }

        public int Iteration { get; set; }

        public bool IsFunctionCall { get; init; }
    }
}
