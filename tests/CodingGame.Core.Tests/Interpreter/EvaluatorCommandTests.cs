using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;
using Xunit;
using static CodingGame.Core.Tests.Interpreter.EvaluatorTestSupport;

namespace CodingGame.Core.Tests.Interpreter;

public class EvaluatorCommandTests
{
    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    [Fact]
    public void EmptyProgramFinishesAtOnceWithoutTicks()
    {
        Evaluator evaluator = NewEvaluator(NewField());

        Assert.Equal(StepStatus.Finished, evaluator.Step().Status);
        Assert.Equal(0, evaluator.TicksUsed);
        Assert.Equal(0, evaluator.CurrentLine);
    }

    [Fact]
    public void MoveTakesEffectOnTheFirstTick()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(field, CallStatement(1, "move", Constant(1, "EAST")));

        StepResult step = evaluator.Step();

        Assert.Equal(StepStatus.Running, step.Status);
        Assert.Equal(1, evaluator.TicksUsed);
        Assert.Equal(new TilePosition(1, 0), field.DronePosition);
        Assert.Equal(1, evaluator.CurrentLine);
    }

    [Fact]
    public void MoveRunsForTwentyTicksAndThenFinishes()
    {
        Evaluator evaluator = NewEvaluator(NewField(), CallStatement(1, "move", Constant(1, "EAST")));

        for (int tick = 0; tick < 20; tick++)
        {
            Assert.Equal(StepStatus.Running, evaluator.Step().Status);
        }

        Assert.Equal(20, evaluator.TicksUsed);
        Assert.Equal(StepStatus.Finished, evaluator.Step().Status);
        Assert.Equal(20, evaluator.TicksUsed);
    }

    [Fact]
    public void HarvestThenMoveTakesFortyTicks()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(
            field,
            CallStatement(1, "harvest"),
            CallStatement(2, "move", Constant(2, "EAST")));

        StepResult result = RunToEnd(evaluator);

        Assert.Equal(StepStatus.Finished, result.Status);
        Assert.Equal(40, evaluator.TicksUsed);
        Assert.Equal(1, field.Items["CLOVER"]);
        Assert.Equal(new TilePosition(1, 0), field.DronePosition);
    }

    [Fact]
    public void CurrentLineIsTheSecondStatementAfterTwentyOneSteps()
    {
        Evaluator evaluator = NewEvaluator(
            NewField(),
            CallStatement(1, "harvest"),
            CallStatement(2, "move", Constant(2, "EAST")));

        for (int step = 0; step < 21; step++)
        {
            evaluator.Step();
        }

        Assert.Equal(2, evaluator.CurrentLine);
    }

    [Fact]
    public void HarvestEffectComesBeforeTheTicksAdvanceTheCrop()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(field, CallStatement(1, "harvest"));

        evaluator.Step();
        Assert.Equal(1, field.GetTile(0, 0).CropAgeTicks);

        RunToEnd(evaluator);
        Assert.Equal(20, field.GetTile(0, 0).CropAgeTicks);
    }

    [Fact]
    public void PlantSequenceTakesOneHundredTwentyTicks()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(
            field,
            CallStatement(1, "harvest"),
            CallStatement(2, "move", Constant(2, "EAST")),
            CallStatement(3, "harvest"),
            CallStatement(4, "till"),
            CallStatement(5, "till"),
            CallStatement(6, "plant", Constant(6, "RADISH")));

        StepResult result = RunToEnd(evaluator);

        Assert.Equal(StepStatus.Finished, result.Status);
        Assert.Equal(120, evaluator.TicksUsed);
        Assert.Equal(new Tile(Ground.Grass, CropNamed("RADISH"), 20), field.GetTile(1, 0));
        Assert.False(field.Items.ContainsKey("CLOVER"));
    }

    [Fact]
    public void PlantOnOccupiedTileFailsOnTheFirstStep()
    {
        Evaluator evaluator = NewEvaluator(NewField(), CallStatement(1, "plant", Constant(1, "RADISH")));

        StepResult step = evaluator.Step();

        Assert.Equal(StepStatus.Failed, step.Status);
        Assert.Equal(
            new ScriptError(ScriptErrorKind.CommandFailed, 1, "cannot plant RADISH: the tile already has a crop"),
            step.Error);
        Assert.Equal(0, evaluator.TicksUsed);
    }

    [Fact]
    public void FailureStaysOnTheNextStep()
    {
        Evaluator evaluator = NewEvaluator(NewField(), CallStatement(1, "plant", Constant(1, "RADISH")));
        StepResult first = evaluator.Step();

        StepResult second = evaluator.Step();

        Assert.Equal(first, second);
    }

    [Fact]
    public void UnknownCommandIsUndefinedName()
    {
        Evaluator evaluator = NewEvaluator(
            NewField(),
            CallStatement(1, "harvest"),
            CallStatement(3, "mvoe", Constant(3, "NORTH")));

        StepResult result = RunToEnd(evaluator);

        Assert.Equal(StepStatus.Failed, result.Status);
        Assert.Equal(new ScriptError(ScriptErrorKind.UndefinedName, 3, "mvoe"), result.Error);
    }

    [Fact]
    public void UnknownConstantIsUndefinedName()
    {
        Evaluator evaluator = NewEvaluator(NewField(), CallStatement(2, "move", Constant(2, "NORTH_")));

        StepResult result = evaluator.Step();

        Assert.Equal(new ScriptError(ScriptErrorKind.UndefinedName, 2, "NORTH_"), result.Error);
        Assert.Equal(0, evaluator.TicksUsed);
    }

    [Fact]
    public void NumberArgumentToMoveIsWrongArgumentAndChangesNothing()
    {
        Field field = NewField();
        Evaluator evaluator = NewEvaluator(
            field,
            CallStatement(1, "move", new Literal(1, Value.FromNumber(5))));

        StepResult result = evaluator.Step();

        Assert.Equal(new ScriptError(ScriptErrorKind.WrongArgument, 1, "move expects direction, got number"), result.Error);
        Assert.Equal(new TilePosition(0, 0), field.DronePosition);
        Assert.Equal(0, evaluator.TicksUsed);
    }

    [Fact]
    public void DirectionArgumentToPlantIsWrongArgument()
    {
        Evaluator evaluator = NewEvaluator(NewField(), CallStatement(1, "plant", Constant(1, "NORTH")));

        StepResult result = evaluator.Step();

        Assert.Equal(new ScriptError(ScriptErrorKind.WrongArgument, 1, "plant expects crop, got direction"), result.Error);
    }

    [Fact]
    public void MissingArgumentToMoveIsWrongArgument()
    {
        Evaluator evaluator = NewEvaluator(NewField(), CallStatement(1, "move"));

        StepResult result = evaluator.Step();

        Assert.Equal(new ScriptError(ScriptErrorKind.WrongArgument, 1, "move expects 1 argument(s), got 0"), result.Error);
    }

    [Fact]
    public void ExtraArgumentToHarvestIsWrongArgument()
    {
        Evaluator evaluator = NewEvaluator(NewField(), CallStatement(1, "harvest", Constant(1, "NORTH")));

        StepResult result = evaluator.Step();

        Assert.Equal(new ScriptError(ScriptErrorKind.WrongArgument, 1, "harvest expects 0 argument(s), got 1"), result.Error);
    }

    [Fact]
    public void ConstantAsStatementFinishesWithoutTicks()
    {
        Evaluator evaluator = NewEvaluator(NewField(), new ExpressionStatement(1, Constant(1, "NORTH")));

        Assert.Equal(StepStatus.Finished, evaluator.Step().Status);
        Assert.Equal(0, evaluator.TicksUsed);
    }

    [Fact]
    public void LiteralAsStatementFinishesWithoutTicks()
    {
        Evaluator evaluator = NewEvaluator(NewField(), new ExpressionStatement(1, new Literal(1, Value.FromNumber(3))));

        Assert.Equal(StepStatus.Finished, evaluator.Step().Status);
        Assert.Equal(0, evaluator.TicksUsed);
    }

    [Fact]
    public void RipeAsStatementCostsOneTick()
    {
        Evaluator evaluator = NewEvaluator(NewField(), CallStatement(1, "ripe"));

        Assert.Equal(StepStatus.Running, evaluator.Step().Status);
        Assert.Equal(1, evaluator.TicksUsed);
        Assert.Equal(StepStatus.Finished, evaluator.Step().Status);
    }

    private static Crop CropNamed(string name)
    {
        return _rules.Crops.Single(crop => crop.Name == name);
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
