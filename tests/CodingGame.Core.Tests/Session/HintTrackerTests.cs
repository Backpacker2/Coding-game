using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using CodingGame.Core.Tests.Interpreter;
using Xunit;

namespace CodingGame.Core.Tests.Session;

public class HintTrackerTests
{
    private const string UndefinedNameHint = "Check the spelling. Help lists every name you can use.";
    private const string SyntaxHint = "Compare the line with the example in Help.";
    private const string SeedHint = "Planting costs items. Harvest more first.";
    private const string GenericCommandHint = "Read the message after the colon: it says which rule stopped the command.";
    private const int MaxStepsInTest = 10_000;

    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    [Fact]
    public void FirstAndSecondIdenticalErrorGiveNoHint()
    {
        var tracker = new HintTracker(_rules);
        var progress = EvaluatorTestSupport.AllUnlocked(_rules);

        string?[] hints = [RecordRun(tracker, progress, "nope()"), RecordRun(tracker, progress, "nope()")];

        Assert.Equal(new string?[] { null, null }, hints);
    }

    [Fact]
    public void ThirdIdenticalErrorGivesTheHint()
    {
        var tracker = new HintTracker(_rules);
        var progress = EvaluatorTestSupport.AllUnlocked(_rules);
        RecordRun(tracker, progress, "nope()");
        RecordRun(tracker, progress, "nope()");

        Assert.Equal(UndefinedNameHint, RecordRun(tracker, progress, "nope()"));
    }

    [Fact]
    public void FourthIdenticalErrorStillGivesTheHint()
    {
        var tracker = new HintTracker(_rules);
        var progress = EvaluatorTestSupport.AllUnlocked(_rules);
        RecordRun(tracker, progress, "nope()");
        RecordRun(tracker, progress, "nope()");
        RecordRun(tracker, progress, "nope()");

        Assert.Equal(UndefinedNameHint, RecordRun(tracker, progress, "nope()"));
    }

    [Fact]
    public void DifferentDetailStartsTheCountAgain()
    {
        var tracker = new HintTracker(_rules);
        var progress = EvaluatorTestSupport.AllUnlocked(_rules);
        RecordRun(tracker, progress, "nope()");
        RecordRun(tracker, progress, "nope()");
        RecordRun(tracker, progress, "other()");
        RecordRun(tracker, progress, "other()");

        Assert.Equal(UndefinedNameHint, RecordRun(tracker, progress, "other()"));
    }

    [Fact]
    public void DifferentDetailGivesNoHintAtItsSecondRun()
    {
        var tracker = new HintTracker(_rules);
        var progress = EvaluatorTestSupport.AllUnlocked(_rules);
        RecordRun(tracker, progress, "nope()");
        RecordRun(tracker, progress, "nope()");

        Assert.Null(RecordRun(tracker, progress, "other()"));
    }

    [Fact]
    public void RunWithoutErrorResetsTheCount()
    {
        var tracker = new HintTracker(_rules);
        var progress = EvaluatorTestSupport.AllUnlocked(_rules);
        RecordRun(tracker, progress, "nope()");
        RecordRun(tracker, progress, "nope()");
        RecordRun(tracker, progress, "move(EAST)");

        Assert.Null(RecordRun(tracker, progress, "nope()"));
    }

    [Fact]
    public void FailedParseCountsLikeAnyOtherError()
    {
        var tracker = new HintTracker(_rules);
        var progress = EvaluatorTestSupport.AllUnlocked(_rules);
        RecordRun(tracker, progress, "move(EAST");
        RecordRun(tracker, progress, "move(EAST");

        Assert.Equal(SyntaxHint, RecordRun(tracker, progress, "move(EAST"));
    }

    [Fact]
    public void ErrorWithoutAnyHintGivesNullEvenAtThreeRepeats()
    {
        var tracker = new HintTracker(_rules);
        var progress = EvaluatorTestSupport.AllUnlocked(_rules);
        RecordRun(tracker, progress, "move(1)");
        RecordRun(tracker, progress, "move(1)");

        Assert.Null(RecordRun(tracker, progress, "move(1)"));
    }

    [Fact]
    public void KindWithoutMatchingDetailUsesTheHintForTheKindWithoutDetail()
    {
        var tracker = new HintTracker(_rules);
        var progress = EvaluatorTestSupport.AllUnlocked(_rules);
        RecordRun(tracker, progress, "plant(CLOVER)");
        RecordRun(tracker, progress, "plant(CLOVER)");

        Assert.Equal(GenericCommandHint, RecordRun(tracker, progress, "plant(CLOVER)"));
    }

    [Fact]
    public void MatchingDetailGivesItsOwnHint()
    {
        var tracker = new HintTracker(_rules);
        var progress = EvaluatorTestSupport.AllUnlocked(_rules);
        RecordRun(tracker, progress, "plant(RADISH)");
        RecordRun(tracker, progress, "plant(RADISH)");

        Assert.Equal(SeedHint, RecordRun(tracker, progress, "plant(RADISH)"));
    }

    [Fact]
    public void ThirdCompoundAssignmentGivesTheAddOneHint()
    {
        var tracker = new HintTracker(_rules);
        var progress = EvaluatorTestSupport.AllUnlocked(_rules);
        const string script = "count = 0\ncount += 1";
        RecordRun(tracker, progress, script);
        RecordRun(tracker, progress, script);

        Assert.Equal("Write count = count + 1 to add one.", RecordRun(tracker, progress, script));
    }

    private static string? RecordRun(HintTracker tracker, GameProgress progress, string script)
    {
        RunSession session = RunSession.Start(script, ScriptLanguage.Python, progress);
        for (int step = 0; step < MaxStepsInTest && session.Status == StepStatus.Running; step++)
        {
            session.StepTick();
        }

        return tracker.RecordRunEnd(session);
    }
}
