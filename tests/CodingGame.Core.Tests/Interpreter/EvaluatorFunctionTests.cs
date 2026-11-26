using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;
using Xunit;
using static CodingGame.Core.Tests.Interpreter.EvaluatorTestSupport;

namespace CodingGame.Core.Tests.Interpreter;

public class EvaluatorFunctionTests
{
    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    [Fact]
    public void DefinitionCostsNothingAndCallRunsTheBody()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(
            field,
            new FunctionDefinition(1, "f", [CallStatement(2, "move", Constant(2, "EAST"))]),
            CallStatement(4, "f"));

        StepResult first = evaluator.Step();

        Assert.Equal(StepStatus.Running, first.Status);
        Assert.Equal(1, evaluator.TicksUsed);
        Assert.Equal(4, evaluator.CurrentLine);
        Assert.Equal(new TilePosition(0, 0), field.DronePosition);

        Assert.Equal(StepStatus.Running, evaluator.Step().Status);
        Assert.Equal(2, evaluator.CurrentLine);
        Assert.Equal(new TilePosition(1, 0), field.DronePosition);

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(21, evaluator.TicksUsed);
    }

    [Fact]
    public void CallingTwiceRunsTheBodyTwice()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(
            field,
            new FunctionDefinition(1, "f", [CallStatement(2, "move", Constant(2, "EAST"))]),
            CallStatement(4, "f"),
            CallStatement(5, "f"));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(42, evaluator.TicksUsed);
        Assert.Equal(new TilePosition(2, 0), field.DronePosition);
    }

    [Fact]
    public void ControlReturnsToTheCallerAfterTheBody()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(
            field,
            new FunctionDefinition(1, "f", [CallStatement(2, "harvest")]),
            CallStatement(3, "f"),
            CallStatement(4, "move", Constant(4, "EAST")));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(41, evaluator.TicksUsed);
        Assert.Equal(1, field.Items["CLOVER"]);
        Assert.Equal(new TilePosition(1, 0), field.DronePosition);
    }

    [Fact]
    public void FunctionCallingFunctionTakesTwoCallTicks()
    {
        Evaluator evaluator = NewEvaluator(
            NewField(),
            new FunctionDefinition(1, "f", [CallStatement(2, "move", Constant(2, "EAST"))]),
            new FunctionDefinition(3, "g", [CallStatement(4, "f")]),
            CallStatement(6, "g"));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(22, evaluator.TicksUsed);
    }

    [Fact]
    public void CallInsideRepeatRunsEveryIteration()
    {
        Evaluator evaluator = NewEvaluator(
            NewField(),
            new FunctionDefinition(1, "f", [CallStatement(2, "move", Constant(2, "EAST"))]),
            new Repeat(4, new Literal(4, Value.FromNumber(2)), [CallStatement(5, "f")], null));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(44, evaluator.TicksUsed);
    }

    [Fact]
    public void LaterDefinitionReplacesEarlierOne()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(
            field,
            new FunctionDefinition(1, "f", [CallStatement(2, "harvest")]),
            new FunctionDefinition(3, "f", [CallStatement(4, "move", Constant(4, "EAST"))]),
            CallStatement(5, "f"));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(21, evaluator.TicksUsed);
        Assert.Equal(new TilePosition(1, 0), field.DronePosition);
        Assert.False(field.Items.ContainsKey("CLOVER"));
    }

    [Fact]
    public void FunctionShadowsCommandOfTheSameName()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(
            field,
            new FunctionDefinition(1, "move", [CallStatement(2, "harvest")]),
            CallStatement(3, "move"));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(21, evaluator.TicksUsed);
        Assert.Equal(1, field.Items["CLOVER"]);
    }

    [Fact]
    public void CallBeforeDefinitionIsUndefinedName()
    {
        Evaluator evaluator = NewEvaluator(
            NewField(),
            CallStatement(1, "f"),
            new FunctionDefinition(2, "f", [CallStatement(3, "harvest")]));

        StepResult result = evaluator.Step();

        Assert.Equal(StepStatus.Failed, result.Status);
        Assert.Equal(new ScriptError(ScriptErrorKind.UndefinedName, 1, "f"), result.Error);
        Assert.Equal(0, evaluator.TicksUsed);
    }

    [Fact]
    public void ArgumentToFunctionIsWrongArgument()
    {
        Evaluator evaluator = NewEvaluator(
            NewField(),
            new FunctionDefinition(1, "f", [CallStatement(2, "harvest")]),
            CallStatement(3, "f", Constant(3, "NORTH")));

        StepResult result = evaluator.Step();

        Assert.Equal(
            new ScriptError(ScriptErrorKind.WrongArgument, 3, "f expects 0 argument(s), got 1"),
            result.Error);
    }

    [Fact]
    public void FunctionCallingItselfEndsAtTheCallDepthLimit()
    {
        Evaluator evaluator = NewEvaluator(
            NewField(),
            new FunctionDefinition(1, "f", [CallStatement(2, "f")]),
            CallStatement(3, "f"));

        for (int step = 0; step < Evaluator.MaxCallDepth; step++)
        {
            Assert.Equal(StepStatus.Running, evaluator.Step().Status);
        }

        StepResult result = evaluator.Step();

        Assert.Equal(StepStatus.Failed, result.Status);
        Assert.Equal(new ScriptError(ScriptErrorKind.TooDeep, 2, "f"), result.Error);
        Assert.Equal(Evaluator.MaxCallDepth, evaluator.TicksUsed);
    }

    [Fact]
    public void ChainOfCallsUpToTheCallDepthLimitSucceeds()
    {
        var statements = new List<Statement>();
        for (int index = 0; index < Evaluator.MaxCallDepth; index++)
        {
            string nextCallee = index + 1 < Evaluator.MaxCallDepth ? $"f{index + 1}" : "harvest";
            statements.Add(new FunctionDefinition(index + 1, $"f{index}", [CallStatement(index + 1, nextCallee)]));
        }
        statements.Add(CallStatement(Evaluator.MaxCallDepth + 1, "f0"));
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(field, statements.ToArray());

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(Evaluator.MaxCallDepth + 20, evaluator.TicksUsed);
        Assert.Equal(1, field.Items["CLOVER"]);
    }

    [Fact]
    public void FunctionCallInConditionIsWrongArgument()
    {
        Evaluator evaluator = NewEvaluator(
            NewField(),
            new FunctionDefinition(1, "f", [CallStatement(2, "move", Constant(2, "EAST"))]),
            new If(3, new Call(3, "f", []), [CallStatement(4, "harvest")], []));

        StepResult result = RunToEnd(evaluator);

        Assert.Equal(StepStatus.Failed, result.Status);
        Assert.Equal(
            new ScriptError(ScriptErrorKind.WrongArgument, 3, "f returns nothing and cannot be used in an expression"),
            result.Error);
    }

    private static Field NewField()
    {
        return EvaluatorTestSupport.NewField(_rules);
    }

    private static Evaluator NewEvaluator(Field field, params Statement[] body)
    {
        return new Evaluator(PythonProgram(body), new DroneApi(_rules, field));
    }
}
