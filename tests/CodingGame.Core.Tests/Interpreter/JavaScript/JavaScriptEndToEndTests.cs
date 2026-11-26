using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.JavaScript;
using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;
using Xunit;
using static CodingGame.Core.Tests.Interpreter.EvaluatorTestSupport;

namespace CodingGame.Core.Tests.Interpreter.JavaScript;

public class JavaScriptEndToEndTests
{
    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    [Fact]
    public void FunctionCalledBeforeItsDefinitionRunsBecauseItIsHoisted()
    {
        Field field = NewField(_rules);

        (StepResult result, Evaluator evaluator) = Run("f();\nfunction f() {\n    move(EAST);\n}", field);

        Assert.Equal(StepStatus.Finished, result.Status);
        Assert.Equal(21, evaluator.TicksUsed);
        Assert.Equal(new TilePosition(1, 0), field.DronePosition);
    }

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
        function farmTile() {
            if (ripe()) {
                harvest();
                if (ground() === SOIL) {
                    plant(POTATO);
                } else {
                    plant(RADISH);
                }
            }
        }

        move(EAST);
        move(EAST);
        farmTile();
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
        function farmTile() {
            if (ripe()) {
                harvest();
                if (ground() === SOIL) {
                    plant(POTATO);
                } else {
                    plant(RADISH);
                }
            }
        }

        for (let y = 0; y < 3; y++) {
            for (let x = 0; x < 3; x++) {
                farmTile();
                move(EAST);
            }
            move(NORTH);
        }
        """;

        (StepResult result, _) = Run(source, field);

        Assert.Equal(StepStatus.Failed, result.Status);
        var expected = new ScriptError(ScriptErrorKind.CommandFailed, 7, "cannot plant RADISH: not enough seed items");
        Assert.Equal(expected, result.Error);
        Assert.Equal(
            "Error on line 7: cannot plant RADISH: not enough seed items",
            result.Error!.ToMessage(ScriptLanguage.JavaScript));
    }

    [Fact]
    public void LoopVariableIsNotDefined()
    {
        Field field = NewField(_rules);

        (StepResult result, _) = Run("for (let i = 0; i < 2; i++) {\n    move(i);\n}", field);

        Assert.Equal(StepStatus.Failed, result.Status);
        Assert.Equal(new ScriptError(ScriptErrorKind.UndefinedName, 2, "i"), result.Error);
        Assert.Equal(
            "ReferenceError on line 2: i is not defined",
            result.Error!.ToMessage(ScriptLanguage.JavaScript));
    }

    [Fact]
    public void UndefinedCommandFailsAtRunTime()
    {
        Field field = NewField(_rules);

        (StepResult result, _) = Run("mvoe(EAST);", field);

        Assert.Equal(StepStatus.Failed, result.Status);
        Assert.Equal(
            "ReferenceError on line 1: mvoe is not defined",
            result.Error!.ToMessage(ScriptLanguage.JavaScript));
    }

    [Fact]
    public void MissingSemicolonIsASyntaxError()
    {
        ParseResult result = JavaScriptFrontEnd.Parse("harvest()");

        Assert.Equal(
            "SyntaxError on line 1: expected ';'",
            result.Error!.ToMessage(ScriptLanguage.JavaScript));
    }

    [Fact]
    public void MissingBracesAreASyntaxError()
    {
        ParseResult result = JavaScriptFrontEnd.Parse("if (ripe())\n    harvest();");

        Assert.Equal(
            "SyntaxError on line 2: expected '{', braces are required around the body",
            result.Error!.ToMessage(ScriptLanguage.JavaScript));
    }

    [Fact]
    public void ImportIsNotUnlockedYet()
    {
        ParseResult result = JavaScriptFrontEnd.Parse("import os;");

        Assert.Equal(
            "Error on line 1: 'import' is not unlocked yet",
            result.Error!.ToMessage(ScriptLanguage.JavaScript));
    }

    [Fact]
    public void LooseEqualityIsRejectedWithAnExplanation()
    {
        ParseResult result = JavaScriptFrontEnd.Parse("if (ripe() == true) {\n}");

        Assert.Equal(
            "SyntaxError on line 1: '==' is not allowed, use '===': '==' converts types before it compares",
            result.Error!.ToMessage(ScriptLanguage.JavaScript));
    }

    [Fact]
    public void SpinCostsSpinTicksAndChangesOnlyTheSpinCount()
    {
        Field withSpin = NewField(_rules);
        Field withoutSpin = NewField(_rules);

        (StepResult spinResult, Evaluator spinEvaluator) = Run("spin();\nmove(EAST);", withSpin);
        (StepResult plainResult, Evaluator plainEvaluator) = Run("move(EAST);", withoutSpin);

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

        (StepResult result, _) = Run("spin(EAST);", field);

        Assert.Equal(StepStatus.Failed, result.Status);
        Assert.Equal(new ScriptError(ScriptErrorKind.WrongArgument, 1, "spin expects 0 argument(s), got 1"), result.Error);
        Assert.Equal(
            "TypeError on line 1: spin expects 0 argument(s), got 1",
            result.Error!.ToMessage(ScriptLanguage.JavaScript));
        Assert.Equal(0, field.SpinCount);
    }

    private static (StepResult Result, Evaluator Evaluator) Run(string source, Field field)
    {
        ParseResult parsed = JavaScriptFrontEnd.Parse(source);
        Assert.Null(parsed.Error);
        var evaluator = new Evaluator(parsed.Program!, new DroneApi(_rules, field));
        return (RunToEnd(evaluator), evaluator);
    }
}
