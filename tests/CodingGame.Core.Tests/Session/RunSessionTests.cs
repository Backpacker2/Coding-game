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
    private const int MaxStepsInTest = 1000;

    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    [Fact]
    public void NewFieldUsesStartFieldAndWildCrop()
    {
        Field field = RunSession.NewField(_rules);

        Assert.Equal(3, field.Width);
        Assert.Equal(3, field.Height);
        Assert.Equal("CLOVER", field.GetTile(0, 0).Crop?.Name);
    }

    [Fact]
    public void StartRunsNothingYet()
    {
        RunSession session = RunSession.Start(PythonThreeLines, ScriptLanguage.Python, _rules);

        Assert.Equal(StepStatus.Running, session.Status);
        Assert.Equal(0, session.TicksUsed);
        Assert.Equal(0, session.CurrentLine);
        Assert.Equal(new TilePosition(0, 0), session.Field.DronePosition);
    }

    [Fact]
    public void StepTickUsesOneTick()
    {
        RunSession session = RunSession.Start(PythonThreeLines, ScriptLanguage.Python, _rules);

        session.StepTick();

        Assert.Equal(1, session.TicksUsed);
        Assert.Equal(1, session.CurrentLine);
    }

    [Fact]
    public void StepLineRunsUntilNextStatement()
    {
        RunSession session = RunSession.Start(PythonThreeLines, ScriptLanguage.Python, _rules);

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
        RunSession session = RunSession.Start(PythonThreeLines, ScriptLanguage.Python, _rules);
        RunToEnd(session);

        session.StepLine();

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(60, session.TicksUsed);
        Assert.Equal(2, session.Field.Items["CLOVER"]);
    }

    [Fact]
    public void PythonSyntaxErrorFailsAtStart()
    {
        RunSession session = RunSession.Start("harvest(", ScriptLanguage.Python, _rules);

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
        RunSession python = RunSession.Start("plant(RADISH)", ScriptLanguage.Python, _rules);
        python.StepLine();

        RunSession javaScript = RunSession.Start("plant(RADISH);", ScriptLanguage.JavaScript, _rules);
        javaScript.StepLine();

        Assert.Equal(StepStatus.Failed, python.Status);
        Assert.StartsWith("RuntimeError on line 1:", python.ErrorMessage);
        Assert.Equal(StepStatus.Failed, javaScript.Status);
        Assert.StartsWith("Error on line 1:", javaScript.ErrorMessage);
    }

    [Fact]
    public void SameScriptSameResultInBothLanguages()
    {
        RunSession python = RunSession.Start(PythonThreeLines, ScriptLanguage.Python, _rules);
        RunSession javaScript = RunSession.Start(JavaScriptThreeLines, ScriptLanguage.JavaScript, _rules);
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
        RunSession session = RunSession.Start("while (ripe()) {}", ScriptLanguage.JavaScript, _rules);

        session.StepLine();

        Assert.Equal(1, session.CurrentLine);

        session.StepLine();

        Assert.Equal(StepStatus.Running, session.Status);
        Assert.Equal(1 + RunSession.MaxTicksPerLineStep, session.TicksUsed);
    }

    private static void RunToEnd(RunSession session)
    {
        for (int step = 0; step < MaxStepsInTest && session.Status == StepStatus.Running; step++)
        {
            session.StepTick();
        }
    }
}
