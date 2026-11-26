using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.Python;
using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;
using Xunit;
using static CodingGame.Core.Tests.Interpreter.EvaluatorTestSupport;

namespace CodingGame.Core.Tests.Interpreter.Python;

public class PythonEndToEndTests
{
    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    [Fact]
    public void FarmExampleFromTheLanguageDocumentPlantsRadishAfterRipeHarvest()
    {
        Field field = NewField(_rules);
        Crop radish = _rules.Crops.Single(crop => crop.Name == "RADISH");
        for (int harvest = 0; harvest < 4; harvest++)
        {
            if (harvest > 0)
            {
                field.Advance(200);
            }

            field.Harvest();
        }

        field.Move(Direction.East);
        field.Move(Direction.East);
        field.Till();
        field.Till();
        field.Plant(radish);
        field.Advance(300);
        field.Move(Direction.West);
        field.Move(Direction.West);

        const string source = """
        def farm_tile():
            if ripe():
                harvest()
                if ground() == SOIL:
                    plant(POTATO)
                else:
                    plant(RADISH)

        move(EAST)
        move(EAST)
        farm_tile()
        """;

        (StepResult result, Evaluator evaluator) = Run(source, field);

        Assert.Equal(StepStatus.Finished, result.Status);
        Assert.Equal(83, evaluator.TicksUsed);
        Assert.Equal(new TilePosition(2, 0), field.DronePosition);
        Assert.Equal(4, field.Items["RADISH"]);
        Assert.Equal(0, field.Items.GetValueOrDefault("CLOVER"));
        Assert.Equal("RADISH", field.GetTile(2, 0).Crop!.Name);
    }

    [Fact]
    public void FarmExampleFailsWhenTooFewSeedsAreLeftForAWildTile()
    {
        Field field = NewField(_rules);
        const string source = """
        def farm_tile():
            if ripe():
                harvest()
                if ground() == SOIL:
                    plant(POTATO)
                else:
                    plant(RADISH)

        for y in range(3):
            for x in range(3):
                farm_tile()
                move(EAST)
            move(NORTH)
        """;

        (StepResult result, _) = Run(source, field);

        Assert.Equal(StepStatus.Failed, result.Status);
        var expected = new ScriptError(ScriptErrorKind.CommandFailed, 7, "cannot plant RADISH: not enough seed items");
        Assert.Equal(expected, result.Error);
        Assert.Equal(
            "RuntimeError on line 7: cannot plant RADISH: not enough seed items",
            result.Error!.ToMessage(ScriptLanguage.Python));
    }

    [Fact]
    public void LoopVariableIsNotDefined()
    {
        Field field = NewField(_rules);

        (StepResult result, _) = Run("for i in range(2):\n    move(i)", field);

        Assert.Equal(StepStatus.Failed, result.Status);
        Assert.Equal(new ScriptError(ScriptErrorKind.UndefinedName, 2, "i"), result.Error);
        Assert.Equal(
            "NameError on line 2: name 'i' is not defined",
            result.Error!.ToMessage(ScriptLanguage.Python));
    }

    [Fact]
    public void UndefinedCommandFailsAtRunTime()
    {
        Field field = NewField(_rules);

        (StepResult result, _) = Run("mvoe(EAST)", field);

        Assert.Equal(StepStatus.Failed, result.Status);
        Assert.Equal(
            "NameError on line 1: name 'mvoe' is not defined",
            result.Error!.ToMessage(ScriptLanguage.Python));
    }

    [Fact]
    public void MissingColonIsASyntaxError()
    {
        ParseResult result = PythonFrontEnd.Parse("if ripe()\n    harvest()");

        Assert.Equal(
            "SyntaxError on line 1: expected ':'",
            result.Error!.ToMessage(ScriptLanguage.Python));
    }

    [Fact]
    public void MissingIndentIsAnIndentationError()
    {
        ParseResult result = PythonFrontEnd.Parse("if ripe():\nharvest()");

        Assert.Equal(
            "IndentationError on line 2: expected an indented block after 'if' statement on line 1",
            result.Error!.ToMessage(ScriptLanguage.Python));
    }

    [Fact]
    public void ImportIsNotUnlockedYet()
    {
        ParseResult result = PythonFrontEnd.Parse("import os");

        Assert.Equal(
            "Error on line 1: 'import' is not unlocked yet",
            result.Error!.ToMessage(ScriptLanguage.Python));
    }

    [Fact]
    public void SpinCostsSpinTicksAndChangesOnlyTheSpinCount()
    {
        Field withSpin = NewField(_rules);
        Field withoutSpin = NewField(_rules);

        (StepResult spinResult, Evaluator spinEvaluator) = Run("spin()\nmove(EAST)", withSpin);
        (StepResult plainResult, Evaluator plainEvaluator) = Run("move(EAST)", withoutSpin);

        Assert.Equal(StepStatus.Finished, spinResult.Status);
        Assert.Equal(plainResult.Status, spinResult.Status);
        Assert.Equal(plainEvaluator.TicksUsed + _rules.TickCosts.SpinTicks, spinEvaluator.TicksUsed);
        Assert.Equal(withoutSpin.DronePosition, withSpin.DronePosition);
        Assert.Equal(withoutSpin.GetTile(1, 0), withSpin.GetTile(1, 0));
        Assert.Equal(1, withSpin.SpinCount);
        Assert.Equal(0, withoutSpin.SpinCount);
    }

    [Fact]
    public void SpinWithArgumentIsWrongArgument()
    {
        Field field = NewField(_rules);

        (StepResult result, _) = Run("spin(EAST)", field);

        Assert.Equal(StepStatus.Failed, result.Status);
        Assert.Equal(new ScriptError(ScriptErrorKind.WrongArgument, 1, "spin expects 0 argument(s), got 1"), result.Error);
        Assert.Equal(
            "TypeError on line 1: spin expects 0 argument(s), got 1",
            result.Error!.ToMessage(ScriptLanguage.Python));
        Assert.Equal(0, field.SpinCount);
    }

    private static (StepResult Result, Evaluator Evaluator) Run(string source, Field field)
    {
        ParseResult parsed = PythonFrontEnd.Parse(source);
        Assert.Null(parsed.Error);
        var evaluator = new Evaluator(parsed.Program!, new DroneApi(_rules, field));
        return (RunToEnd(evaluator), evaluator);
    }
}
