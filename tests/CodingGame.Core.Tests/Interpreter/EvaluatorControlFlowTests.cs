using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;
using Xunit;
using static CodingGame.Core.Tests.Interpreter.EvaluatorTestSupport;

namespace CodingGame.Core.Tests.Interpreter;

public class EvaluatorControlFlowTests
{
    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    [Fact]
    public void RepeatRunsItsBodyTwice()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(
            field,
            new Repeat(1, new Literal(1, Value.FromNumber(2)), [CallStatement(2, "move", Constant(2, "EAST"))], null));

        StepResult first = evaluator.Step();

        Assert.Equal(StepStatus.Running, first.Status);
        Assert.Equal(1, evaluator.TicksUsed);
        Assert.Equal(1, evaluator.CurrentLine);
        Assert.Equal(new TilePosition(0, 0), field.DronePosition);

        Assert.Equal(StepStatus.Running, evaluator.Step().Status);
        Assert.Equal(new TilePosition(1, 0), field.DronePosition);
        Assert.Equal(2, evaluator.CurrentLine);

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(42, evaluator.TicksUsed);
        Assert.Equal(new TilePosition(2, 0), field.DronePosition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RepeatWithCountOfZeroOrLessRunsNothing(int count)
    {
        Evaluator evaluator = NewEvaluator(
            NewField(),
            new Repeat(1, new Literal(1, Value.FromNumber(count)), [CallStatement(2, "harvest")], null));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(0, evaluator.TicksUsed);
    }

    [Fact]
    public void RepeatWithBooleanCountIsWrongArgument()
    {
        Evaluator evaluator = NewEvaluator(
            NewField(),
            new Repeat(1, new Literal(1, Value.FromBoolean(true)), [CallStatement(2, "harvest")], null));

        StepResult result = evaluator.Step();

        Assert.Equal(StepStatus.Failed, result.Status);
        Assert.Equal(
            new ScriptError(ScriptErrorKind.WrongArgument, 1, "repeat count must be a number, got boolean"),
            result.Error);
    }

    [Fact]
    public void NestedRepeatCountsEveryIteration()
    {
        Evaluator evaluator = NewEvaluator(
            NewField(),
            new Repeat(1, new Literal(1, Value.FromNumber(2)), [
                new Repeat(2, new Literal(2, Value.FromNumber(3)), [CallStatement(3, "ripe")], null),
            ], null));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(14, evaluator.TicksUsed);
    }

    [Fact]
    public void WhileTrueUsesOneTickPerStep()
    {
        Evaluator evaluator = NewEvaluator(NewField(), new While(1, new Literal(1, Value.FromBoolean(true)), []));

        for (int step = 0; step < 1000; step++)
        {
            Assert.Equal(StepStatus.Running, evaluator.Step().Status);
        }

        Assert.Equal(1000, evaluator.TicksUsed);
    }

    [Fact]
    public void WhileRipeHarvestsAndMovesUntilTheTileIsNotRipe()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(
            field,
            new While(1, new Call(1, "ripe", []), [
                CallStatement(2, "harvest"),
                CallStatement(3, "move", Constant(3, "EAST")),
            ]));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(127, evaluator.TicksUsed);
        Assert.Equal(3, field.Items["CLOVER"]);
        Assert.Equal(new TilePosition(0, 0), field.DronePosition);
    }

    [Fact]
    public void WhileFalseRunsNothing()
    {
        Evaluator evaluator = NewEvaluator(
            NewField(),
            new While(1, new Literal(1, Value.FromBoolean(false)), [CallStatement(2, "harvest")]));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(0, evaluator.TicksUsed);
    }

    [Fact]
    public void IfRunsItsBranchWhenTheConditionIsTrue()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(
            field,
            new If(1, new Call(1, "ripe", []), [CallStatement(2, "harvest")], []));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(21, evaluator.TicksUsed);
        Assert.Equal(1, field.Items["CLOVER"]);
    }

    [Fact]
    public void IfTakesTheElseBranchWhenTheTileIsNotRipeAfterHarvest()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(
            field,
            CallStatement(1, "harvest"),
            new If(
                2,
                new Call(2, "ripe", []),
                [CallStatement(3, "move", Constant(3, "EAST"))],
                [CallStatement(4, "move", Constant(4, "WEST"))]));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(41, evaluator.TicksUsed);
        Assert.Equal(new TilePosition(2, 0), field.DronePosition);
    }

    [Fact]
    public void IfWithoutElseDoesNothingWhenTheConditionIsFalse()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(
            field,
            CallStatement(1, "harvest"),
            new If(2, new Call(2, "ripe", []), [CallStatement(3, "move", Constant(3, "EAST"))], []));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(21, evaluator.TicksUsed);
        Assert.Equal(new TilePosition(0, 0), field.DronePosition);
    }

    [Fact]
    public void ElifRunsTheBranchOfTheFirstTrueCondition()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(
            field,
            new If(
                1,
                GroundIs(1, CompareOperator.Equal, "SOIL"),
                [CallStatement(2, "harvest")],
                [
                    new If(
                        3,
                        GroundIs(3, CompareOperator.Equal, "GRASS"),
                        [CallStatement(4, "till")],
                        []),
                ]));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(22, evaluator.TicksUsed);
        Assert.Equal(Ground.Soil, field.GetTile(0, 0).Ground);
    }

    [Fact]
    public void IfGroundIsGrassTillsTheTile()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(
            field,
            new If(1, GroundIs(1, CompareOperator.Equal, "GRASS"), [CallStatement(2, "till")], []));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(21, evaluator.TicksUsed);
        Assert.Equal(Ground.Soil, field.GetTile(0, 0).Ground);
    }

    [Fact]
    public void IfGroundIsNotGrassSkipsTheTile()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(
            field,
            new If(1, GroundIs(1, CompareOperator.NotEqual, "GRASS"), [CallStatement(2, "till")], []));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(1, evaluator.TicksUsed);
        Assert.Equal(Ground.Grass, field.GetTile(0, 0).Ground);
    }

    [Theory]
    [InlineData(CompareOperator.Equal, 0)]
    [InlineData(CompareOperator.NotEqual, 20)]
    public void NumberAndBooleanAreNeverEqual(CompareOperator op, int expectedTicks)
    {
        var condition = new Compare(
            1,
            new Literal(1, Value.FromNumber(1)),
            op,
            new Literal(1, Value.FromBoolean(true)));
        Evaluator evaluator = NewEvaluator(NewField(), new If(1, condition, [CallStatement(2, "harvest")], []));

        Assert.Equal(StepStatus.Finished, RunToEnd(evaluator).Status);
        Assert.Equal(expectedTicks, evaluator.TicksUsed);
    }

    [Fact]
    public void NumberConditionIsWrongArgument()
    {
        Evaluator evaluator = NewEvaluator(
            NewField(),
            new If(1, new Literal(1, Value.FromNumber(1)), [CallStatement(2, "harvest")], []));

        StepResult result = evaluator.Step();

        Assert.Equal(StepStatus.Failed, result.Status);
        Assert.Equal(
            new ScriptError(ScriptErrorKind.WrongArgument, 1, "condition must be true or false, got number"),
            result.Error);
    }

    private static Compare GroundIs(int line, CompareOperator op, string constantName)
    {
        return new Compare(line, new Call(line, "ground", []), op, Constant(line, constantName));
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
