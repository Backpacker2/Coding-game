using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using CodingGame.Core.Tests.Interpreter;
using Xunit;

namespace CodingGame.Core.Tests.Session;

public class GameProgressGoalTests
{
    private const string SnakeScript = "move(EAST)\nmove(EAST)\nmove(SOUTH)\nmove(WEST)\nmove(WEST)\nmove(SOUTH)\nmove(EAST)\nmove(EAST)";
    private const int MaxStepsInTest = 10_000;

    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    [Fact]
    public void NewProgressHasNoGoalDoneAndTheFirstGoalIsCurrent()
    {
        var progress = new GameProgress(_rules);

        Assert.All(_rules.Goals, goal => Assert.False(progress.IsDone(goal)));
        Assert.Equal("FINISH_RUN", progress.CurrentGoal?.Id);
    }

    [Fact]
    public void FinishedRunMarksFinishRunDone()
    {
        var progress = new GameProgress(_rules);
        RunSession session = RunSession.Start("move(EAST)", ScriptLanguage.Python, progress);
        RunToEnd(session);

        IReadOnlyList<Goal> done = progress.CheckGoals(session);

        Assert.Equal(["FINISH_RUN"], done.Select(goal => goal.Id));
        Assert.True(progress.IsDone(GoalNamed("FINISH_RUN")));
        Assert.Equal("HAVE_CLOVER", progress.CurrentGoal?.Id);
    }

    [Fact]
    public void StoppedRunDoesNotFinishRun()
    {
        var progress = new GameProgress(_rules);
        RunSession session = RunSession.Start("move(EAST)\nmove(EAST)", ScriptLanguage.Python, progress);
        session.StepTick();

        IReadOnlyList<Goal> done = progress.CheckGoals(session);

        Assert.Empty(done);
        Assert.False(progress.IsDone(GoalNamed("FINISH_RUN")));
    }

    [Fact]
    public void FailedRunDoesNotFinishRun()
    {
        var progress = new GameProgress(_rules);
        RunSession session = RunSession.Start("plant(RADISH)", ScriptLanguage.Python, progress);

        Assert.Empty(progress.CheckGoals(session));
    }

    [Fact]
    public void HaveItemsIsDoneWhenThePlayerHoldsTheCount()
    {
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 3);
        RunSession session = RunSession.Start("move(EAST)", ScriptLanguage.Python, progress);

        IReadOnlyList<Goal> done = progress.CheckGoals(session);

        Assert.Equal(["HAVE_CLOVER"], done.Select(goal => goal.Id));
    }

    [Fact]
    public void HaveItemsIsNotDoneWithTooFewItems()
    {
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 2);
        RunSession session = RunSession.Start("move(EAST)", ScriptLanguage.Python, progress);

        Assert.Empty(progress.CheckGoals(session));
    }

    [Fact]
    public void VisitingEveryTileInOneRunMarksVisitAllTilesAndFinishRun()
    {
        var progress = new GameProgress(_rules);
        RunSession session = RunSession.Start(SnakeScript, ScriptLanguage.Python, progress);
        RunToEnd(session);

        IReadOnlyList<Goal> done = progress.CheckGoals(session);

        Assert.Equal(["FINISH_RUN", "VISIT_ALL_TILES"], done.Select(goal => goal.Id));
    }

    [Fact]
    public void StoppedRunStillCountsForVisitAllTiles()
    {
        var progress = new GameProgress(_rules);
        RunSession session = RunSession.Start(SnakeScript, ScriptLanguage.Python, progress);
        for (int step = 0; step < MaxStepsInTest && !session.VisitedAllTiles; step++)
        {
            session.StepTick();
        }

        IReadOnlyList<Goal> done = progress.CheckGoals(session);

        Assert.Equal(["VISIT_ALL_TILES"], done.Select(goal => goal.Id));
    }

    [Fact]
    public void BuyingAnUnlockMarksBuyUnlockDone()
    {
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 10);

        progress.Buy(UnlockNamed("FOR_LOOP"));

        Assert.True(progress.IsDone(GoalNamed("BUY_UNLOCK")));
    }

    [Fact]
    public void CheckGoalsReturnsEachGoalOnlyOnce()
    {
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 3);
        RunSession session = RunSession.Start("move(EAST)", ScriptLanguage.Python, progress);
        progress.CheckGoals(session);

        IReadOnlyList<Goal> second = progress.CheckGoals(session);

        Assert.Empty(second);
    }

    [Fact]
    public void DoneGoalStaysDoneWhenItsConditionStopsHolding()
    {
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 10);
        RunSession session = RunSession.Start("move(EAST)", ScriptLanguage.Python, progress);
        progress.CheckGoals(session);

        progress.Buy(UnlockNamed("FOR_LOOP"));

        Assert.True(progress.IsDone(GoalNamed("HAVE_CLOVER")));
    }

    [Fact]
    public void CurrentGoalSkipsGoalsThatAreDoneOutOfOrder()
    {
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 10);

        progress.Buy(UnlockNamed("FOR_LOOP"));

        Assert.Equal("FINISH_RUN", progress.CurrentGoal?.Id);
    }

    [Fact]
    public void CurrentGoalIsNullWhenEveryGoalIsDone()
    {
        var doneIds = new HashSet<string>(_rules.Goals.Select(goal => goal.Id));
        var progress = new GameProgress(_rules, new Dictionary<string, int>(), new HashSet<string>(), doneIds, null, default);

        Assert.Null(progress.CurrentGoal);
    }

    [Fact]
    public void ResetClearsDoneGoals()
    {
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 10);
        progress.Buy(UnlockNamed("FOR_LOOP"));

        progress.Reset();

        Assert.False(progress.IsDone(GoalNamed("BUY_UNLOCK")));
        Assert.Equal("FINISH_RUN", progress.CurrentGoal?.Id);
    }

    [Fact]
    public void RestoredProgressKeepsDoneGoals()
    {
        var doneIds = new HashSet<string> { "BUY_UNLOCK" };
        var progress = new GameProgress(_rules, new Dictionary<string, int>(), new HashSet<string>(), doneIds, null, default);

        Assert.True(progress.IsDone(GoalNamed("BUY_UNLOCK")));
        Assert.Equal("FINISH_RUN", progress.CurrentGoal?.Id);
    }

    [Fact]
    public void RestoredProgressRejectsUnknownGoalId()
    {
        var doneIds = new HashSet<string> { "NOPE" };

        Assert.Throws<ArgumentException>(() => new GameProgress(_rules, new Dictionary<string, int>(), new HashSet<string>(), doneIds, null, default));
    }

    private static Goal GoalNamed(string id)
    {
        return _rules.Goals.Single(goal => goal.Id == id);
    }

    private static Unlock UnlockNamed(string id)
    {
        return _rules.Unlocks.Single(unlock => unlock.Id == id);
    }

    private static void RunToEnd(RunSession session)
    {
        for (int step = 0; step < MaxStepsInTest && session.Status == StepStatus.Running; step++)
        {
            session.StepTick();
        }
    }
}
