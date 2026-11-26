using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using CodingGame.Core.Tests.Interpreter;
using Xunit;

namespace CodingGame.Core.Tests.Session;

public class GameProgressRunStatsTests
{
    private const string HarvestTwiceAndMove = "harvest()\nmove(EAST)\nharvest()";
    private const string MoveTwiceAndHarvest = "move(SOUTH)\nmove(SOUTH)\nharvest()";
    private const string HarvestOnce = "harvest()";
    private const string HarvestTwiceAndMoveSouth = "harvest()\nmove(EAST)\nharvest()\nmove(SOUTH)";
    private const int MaxStepsInTest = 10_000;

    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    [Fact]
    public void HarvestingTwoCloverReportsTheRatePerThousandTicks()
    {
        var progress = new GameProgress(_rules);

        RunStats stats = RecordRun(progress, HarvestTwiceAndMove);

        Assert.Equal(60, stats.TicksUsed);
        Assert.Equal([new ItemRate("CLOVER", 2, 33, true)], stats.Rates);
        Assert.Equal(33, progress.BestRates["CLOVER"]);
    }

    [Fact]
    public void SlowerRunIsNotANewBestAndKeepsTheBest()
    {
        var progress = new GameProgress(_rules);
        RecordRun(progress, HarvestTwiceAndMove);

        RunStats slower = RecordRun(progress, MoveTwiceAndHarvest);

        Assert.Equal([new ItemRate("CLOVER", 1, 16, false)], slower.Rates);
        Assert.Equal(33, progress.BestRates["CLOVER"]);
    }

    [Fact]
    public void FasterRunIsANewBest()
    {
        var progress = new GameProgress(_rules);
        RecordRun(progress, HarvestTwiceAndMoveSouth);

        RunStats faster = RecordRun(progress, HarvestOnce);

        Assert.Equal([new ItemRate("CLOVER", 1, 50, true)], faster.Rates);
        Assert.Equal(50, progress.BestRates["CLOVER"]);
    }

    [Fact]
    public void PlantingThatSpendsMoreThanIsGainedReportsNothingForThatItem()
    {
        GameProgress progress = EvaluatorTestSupport.AllUnlocked(_rules);
        progress.AddItems("CLOVER", 1);

        RunStats stats = RecordRun(progress, "harvest()\nplant(RADISH)");

        Assert.Equal(40, stats.TicksUsed);
        Assert.Empty(stats.Rates);
        Assert.Empty(progress.BestRates);
    }

    [Fact]
    public void StoppedRunCountsWithTheTicksItUsed()
    {
        var progress = new GameProgress(_rules);
        RunSession session = RunSession.Start(HarvestTwiceAndMove, ScriptLanguage.Python, progress);
        for (int step = 0; step < 20; step++)
        {
            session.StepTick();
        }

        RunStats stats = progress.RecordRunStats(session);

        Assert.Equal(20, stats.TicksUsed);
        Assert.Equal([new ItemRate("CLOVER", 1, 50, true)], stats.Rates);
    }

    [Fact]
    public void RunThatUsedNoTicksReportsNothing()
    {
        var progress = new GameProgress(_rules);
        RunSession session = RunSession.Start(HarvestOnce, ScriptLanguage.Python, progress);
        progress.AddItems("CLOVER", 3);

        RunStats stats = progress.RecordRunStats(session);

        Assert.Equal(0, stats.TicksUsed);
        Assert.Empty(stats.Rates);
        Assert.Empty(progress.BestRates);
    }

    [Fact]
    public void SameRunFromTheSameStateGivesTheSameStats()
    {
        RunStats first = RecordRun(new GameProgress(_rules), HarvestTwiceAndMove);

        RunStats second = RecordRun(new GameProgress(_rules), HarvestTwiceAndMove);

        Assert.Equal(first.TicksUsed, second.TicksUsed);
        Assert.Equal(first.Rates, second.Rates);
    }

    [Fact]
    public void ResetClearsTheBestRates()
    {
        var progress = new GameProgress(_rules);
        RecordRun(progress, HarvestOnce);

        progress.Reset();

        Assert.Empty(progress.BestRates);
    }

    [Fact]
    public void RestoredProgressKeepsTheBestRates()
    {
        var bests = new Dictionary<string, int> { ["CLOVER"] = 40 };
        var progress = new GameProgress(_rules, new Dictionary<string, int>(), new HashSet<string>(), new HashSet<string>(), bests, null, default);

        Assert.Equal(40, progress.BestRates["CLOVER"]);
    }

    [Theory]
    [InlineData("TURNIP", 5)]
    [InlineData("CLOVER", 0)]
    public void RestoredProgressRejectsBadBestRates(string item, int rate)
    {
        var bests = new Dictionary<string, int> { [item] = rate };

        Assert.Throws<ArgumentException>(() => new GameProgress(_rules, new Dictionary<string, int>(), new HashSet<string>(), new HashSet<string>(), bests, null, default));
    }

    private static RunStats RecordRun(GameProgress progress, string script)
    {
        RunSession session = RunSession.Start(script, ScriptLanguage.Python, progress);
        for (int step = 0; step < MaxStepsInTest && session.Status == StepStatus.Running; step++)
        {
            session.StepTick();
        }

        return progress.RecordRunStats(session);
    }
}
