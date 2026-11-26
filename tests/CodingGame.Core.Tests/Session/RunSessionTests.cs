using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using CodingGame.Core.Simulation;
using CodingGame.Core.Tests.Interpreter;
using Xunit;

namespace CodingGame.Core.Tests.Session;

public class RunSessionTests
{
    private const string PythonThreeLines = "harvest()\nmove(EAST)\nharvest()";
    private const string JavaScriptThreeLines = "harvest();\nmove(EAST);\nharvest();";
    private const string SnakeScript = "move(EAST)\nmove(EAST)\nmove(SOUTH)\nmove(WEST)\nmove(WEST)\nmove(SOUTH)\nmove(EAST)\nmove(EAST)";
    private const int MaxStepsInTest = 1000;

    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    [Fact]
    public void StartUsesStartFieldAndWildCrop()
    {
        RunSession session = RunSession.Start(PythonThreeLines, ScriptLanguage.Python, new GameProgress(_rules));

        Assert.Equal(3, session.Field.Width);
        Assert.Equal(3, session.Field.Height);
        Assert.Equal("CLOVER", session.Field.GetTile(0, 0).Crop?.Name);
    }

    [Fact]
    public void LockedLoopFailsAtStartBeforeAnyTick()
    {
        RunSession session = RunSession.Start("for i in range(2):\n    harvest()", ScriptLanguage.Python, new GameProgress(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(new ScriptError(ScriptErrorKind.NotUnlocked, 1, "for"), session.Error);
        Assert.Equal("Error on line 1: 'for' is not unlocked yet", session.ErrorMessage);
        Assert.Equal(0, session.TicksUsed);
        Assert.Equal(0, session.CurrentLine);
    }

    [Fact]
    public void LockedFunctionShowsJavaScriptWording()
    {
        RunSession session = RunSession.Start("function f() {}", ScriptLanguage.JavaScript, new GameProgress(_rules));

        Assert.Equal("Error on line 1: 'function' is not unlocked yet", session.ErrorMessage);
    }

    [Fact]
    public void LockedPlantFailsAtStart()
    {
        RunSession session = RunSession.Start("plant(RADISH)", ScriptLanguage.Python, new GameProgress(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("Error on line 1: 'plant' is not unlocked yet", session.ErrorMessage);
    }

    [Fact]
    public void UnlockedScriptRunsWithItemsKeptInProgress()
    {
        var progress = new GameProgress(_rules);

        RunSession first = RunSession.Start(PythonThreeLines, ScriptLanguage.Python, progress);
        RunToEnd(first);

        Assert.Equal(StepStatus.Finished, first.Status);
        Assert.Equal(2, progress.Items["CLOVER"]);

        RunSession second = RunSession.Start(PythonThreeLines, ScriptLanguage.Python, progress);
        RunToEnd(second);

        Assert.Equal(StepStatus.Finished, second.Status);
        Assert.Equal(3, progress.Items["CLOVER"]);
    }

    [Fact]
    public void BoughtUnlockLetsLockedScriptRun()
    {
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 10);
        progress.Buy(_rules.Unlocks.Single(unlock => unlock.Id == "FOR_LOOP"));

        RunSession session = RunSession.Start("for i in range(2):\n    harvest()", ScriptLanguage.Python, progress);
        RunToEnd(session);

        Assert.Equal(StepStatus.Finished, session.Status);
    }

    [Fact]
    public void SyntaxErrorStaysSyntaxErrorWithLockedFeatures()
    {
        RunSession session = RunSession.Start("harvest(", ScriptLanguage.Python, new GameProgress(_rules));

        Assert.Equal(ScriptErrorKind.SyntaxError, session.Error?.Kind);
    }

    [Fact]
    public void StartRunsNothingYet()
    {
        RunSession session = RunSession.Start(PythonThreeLines, ScriptLanguage.Python, EvaluatorTestSupport.AllUnlocked(_rules));

        Assert.Equal(StepStatus.Running, session.Status);
        Assert.Equal(0, session.TicksUsed);
        Assert.Equal(0, session.CurrentLine);
        Assert.Equal(new TilePosition(0, 0), session.Field.DronePosition);
    }

    [Fact]
    public void StepTickUsesOneTick()
    {
        RunSession session = RunSession.Start(PythonThreeLines, ScriptLanguage.Python, EvaluatorTestSupport.AllUnlocked(_rules));

        session.StepTick();

        Assert.Equal(1, session.TicksUsed);
        Assert.Equal(1, session.CurrentLine);
    }

    [Fact]
    public void StepLineRunsUntilNextStatement()
    {
        RunSession session = RunSession.Start(PythonThreeLines, ScriptLanguage.Python, EvaluatorTestSupport.AllUnlocked(_rules));

        session.StepLine();

        Assert.Equal(1, session.CurrentLine);
        Assert.Equal(1, session.TicksUsed);

        session.StepLine();

        Assert.Equal(2, session.CurrentLine);
        Assert.Equal(21, session.TicksUsed);
        Assert.Equal(new TilePosition(1, 0), session.Field.DronePosition);
    }

    [Fact]
    public void StepLineAfterEndDoesNothing()
    {
        RunSession session = RunSession.Start(PythonThreeLines, ScriptLanguage.Python, EvaluatorTestSupport.AllUnlocked(_rules));
        RunToEnd(session);

        session.StepLine();

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(60, session.TicksUsed);
        Assert.Equal(2, session.Field.Items["CLOVER"]);
    }

    [Fact]
    public void PythonSyntaxErrorFailsAtStart()
    {
        RunSession session = RunSession.Start("harvest(", ScriptLanguage.Python, EvaluatorTestSupport.AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(ScriptErrorKind.SyntaxError, session.Error?.Kind);
        Assert.Equal(0, session.TicksUsed);
        Assert.StartsWith("SyntaxError on line 1", session.ErrorMessage);

        session.StepTick();

        Assert.Equal(0, session.TicksUsed);
    }

    [Fact]
    public void RuntimeErrorShowsLanguageMessage()
    {
        RunSession python = RunSession.Start("plant(RADISH)", ScriptLanguage.Python, EvaluatorTestSupport.AllUnlocked(_rules));
        python.StepLine();

        RunSession javaScript = RunSession.Start("plant(RADISH);", ScriptLanguage.JavaScript, EvaluatorTestSupport.AllUnlocked(_rules));
        javaScript.StepLine();

        Assert.Equal(StepStatus.Failed, python.Status);
        Assert.StartsWith("RuntimeError on line 1:", python.ErrorMessage);
        Assert.Equal(StepStatus.Failed, javaScript.Status);
        Assert.StartsWith("Error on line 1:", javaScript.ErrorMessage);
    }

    [Fact]
    public void SameScriptSameResultInBothLanguages()
    {
        RunSession python = RunSession.Start(PythonThreeLines, ScriptLanguage.Python, EvaluatorTestSupport.AllUnlocked(_rules));
        RunSession javaScript = RunSession.Start(JavaScriptThreeLines, ScriptLanguage.JavaScript, EvaluatorTestSupport.AllUnlocked(_rules));
        RunToEnd(python);
        RunToEnd(javaScript);

        Assert.Equal(StepStatus.Finished, python.Status);
        Assert.Equal(StepStatus.Finished, javaScript.Status);
        Assert.Equal(python.TicksUsed, javaScript.TicksUsed);
        Assert.Equal(python.Field.DronePosition, javaScript.Field.DronePosition);
    }

    [Fact]
    public void StepLineStopsAtTickCap()
    {
        RunSession session = RunSession.Start("while (ripe()) {}", ScriptLanguage.JavaScript, EvaluatorTestSupport.AllUnlocked(_rules));

        session.StepLine();

        Assert.Equal(1, session.CurrentLine);

        session.StepLine();

        Assert.Equal(StepStatus.Running, session.Status);
        Assert.Equal(1 + RunSession.MaxTicksPerLineStep, session.TicksUsed);
    }

    [Fact]
    public void PlantedCropAndDroneStayOnTheFieldForTheNextRun()
    {
        GameProgress progress = EvaluatorTestSupport.AllUnlocked(_rules);
        progress.AddItems("CLOVER", 2);
        RunSession first = RunSession.Start("till()\ntill()\nplant(RADISH)\nmove(EAST)", ScriptLanguage.Python, progress);
        RunToEnd(first);
        Tile afterFirstRun = progress.Field.GetTile(0, 0);

        RunSession second = RunSession.Start("harvest()", ScriptLanguage.Python, progress);

        Assert.Equal(StepStatus.Running, second.Status);
        Assert.Equal("RADISH", afterFirstRun.Crop?.Name);
        Assert.True(afterFirstRun.CropAgeTicks > 0);
        Assert.Equal(afterFirstRun, second.Field.GetTile(0, 0));
        Assert.Equal(new TilePosition(1, 0), second.Field.DronePosition);
    }

    [Fact]
    public void TwoRunsFromTheSameStateGiveTheSameEndState()
    {
        const string firstScript = "till()\ntill()\nplant(RADISH)\nmove(EAST)";
        const string secondScript = "harvest()\nmove(EAST)";
        GameProgress first = ProgressWithTwoClover();
        GameProgress second = ProgressWithTwoClover();

        RunToEnd(RunSession.Start(firstScript, ScriptLanguage.Python, first));
        RunSession firstSecond = RunSession.Start(secondScript, ScriptLanguage.Python, first);
        RunToEnd(firstSecond);
        RunToEnd(RunSession.Start(firstScript, ScriptLanguage.Python, second));
        RunSession secondSecond = RunSession.Start(secondScript, ScriptLanguage.Python, second);
        RunToEnd(secondSecond);

        Assert.Equal(StepStatus.Finished, secondSecond.Status);
        Assert.Equal(firstSecond.TicksUsed, secondSecond.TicksUsed);
        Assert.Equal(first.Field.DronePosition, second.Field.DronePosition);
        Assert.Equal(first.Items.OrderBy(item => item.Key), second.Items.OrderBy(item => item.Key));
        for (int x = 0; x < first.Field.Width; x++)
        {
            for (int y = 0; y < first.Field.Height; y++)
            {
                Assert.Equal(first.Field.GetTile(x, y), second.Field.GetTile(x, y));
            }
        }
    }

    [Fact]
    public void VisitedAllTilesIsFalseAtTheStartOfAThreeByThreeField()
    {
        RunSession session = RunSession.Start(PythonThreeLines, ScriptLanguage.Python, new GameProgress(_rules));

        Assert.False(session.VisitedAllTiles);
    }

    [Fact]
    public void VisitedAllTilesWhenTheDroneWentOverEveryTile()
    {
        RunSession session = RunSession.Start(SnakeScript, ScriptLanguage.Python, new GameProgress(_rules));

        RunToEnd(session);

        Assert.True(session.VisitedAllTiles);
    }

    [Fact]
    public void VisitedAllTilesIsFalseWhenOneTileWasSkipped()
    {
        const string snakeWithoutLastMove = "move(EAST)\nmove(EAST)\nmove(SOUTH)\nmove(WEST)\nmove(WEST)\nmove(SOUTH)\nmove(EAST)";
        RunSession session = RunSession.Start(snakeWithoutLastMove, ScriptLanguage.Python, new GameProgress(_rules));

        RunToEnd(session);

        Assert.False(session.VisitedAllTiles);
    }

    [Fact]
    public void VisitedAllTilesStartsAgainInEachRun()
    {
        GameProgress progress = new GameProgress(_rules);
        RunToEnd(RunSession.Start(SnakeScript, ScriptLanguage.Python, progress));

        RunSession second = RunSession.Start("harvest()", ScriptLanguage.Python, progress);

        Assert.False(second.VisitedAllTiles);
    }

    private static GameProgress ProgressWithTwoClover()
    {
        GameProgress progress = EvaluatorTestSupport.AllUnlocked(_rules);
        progress.AddItems("CLOVER", 2);
        return progress;
    }

    private static void RunToEnd(RunSession session)
    {
        for (int step = 0; step < MaxStepsInTest && session.Status == StepStatus.Running; step++)
        {
            session.StepTick();
        }
    }
}
