using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using CodingGame.Core.Simulation;
using Xunit;
using static CodingGame.Core.Tests.Interpreter.EvaluatorTestSupport;

namespace CodingGame.Core.Tests.Interpreter.Python;

public class PythonLogicEndToEndTests
{
    private const int MaxTicks = 10_000;
    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    [Theory]
    [InlineData("True and True", true)]
    [InlineData("True and False", false)]
    [InlineData("False and True", false)]
    [InlineData("False and False", false)]
    [InlineData("True or True", true)]
    [InlineData("True or False", true)]
    [InlineData("False or True", true)]
    [InlineData("False or False", false)]
    [InlineData("not True", false)]
    [InlineData("not False", true)]
    [InlineData("not 1 == 2 and 3 == 3", true)]
    [InlineData("not 1 == 1 or 2 == 2", true)]
    [InlineData("True or False and False", true)]
    [InlineData("1 == 1 and 2 == 3 or 4 == 4", true)]
    public void MovesOnlyWhenTheConditionHolds(string condition, bool holds)
    {
        GameProgress progress = AllUnlocked(_rules);
        TilePosition start = progress.Field.DronePosition;

        RunSession session = Run($"if {condition}:\n    move(EAST)", progress);

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(holds ? new TilePosition(start.X + 1, start.Y) : start, progress.Field.DronePosition);
    }

    [Theory]
    [InlineData("if False and ripe():\n    move(EAST)", "if False and True:\n    move(EAST)")]
    [InlineData("if True or ripe():\n    move(EAST)", "if True or True:\n    move(EAST)")]
    public void RightSideIsSkippedWhenTheLeftSideDecides(string skipped, string reference)
    {
        RunSession withRipe = Run(skipped, AllUnlocked(_rules));
        RunSession withoutRipe = Run(reference, AllUnlocked(_rules));

        Assert.Equal(withoutRipe.TicksUsed, withRipe.TicksUsed);
    }

    [Fact]
    public void RightSideCostsItsTicksWhenItIsEvaluated()
    {
        RunSession withRipe = Run("if True and ripe():\n    move(EAST)", AllUnlocked(_rules));
        RunSession withoutRipe = Run("if True and True:\n    move(EAST)", AllUnlocked(_rules));

        Assert.Equal(withoutRipe.TicksUsed + _rules.TickCosts.RipeTicks, withRipe.TicksUsed);
    }

    [Theory]
    [InlineData("1 and True", "TypeError on line 1: and needs true or false on both sides, got a number")]
    [InlineData("True and 1", "TypeError on line 1: and needs true or false on both sides, got a number")]
    [InlineData("False or EAST", "TypeError on line 1: or needs true or false on both sides, got a direction")]
    [InlineData("not 3", "TypeError on line 1: not needs true or false, got a number")]
    [InlineData("not EAST", "TypeError on line 1: not needs true or false, got a direction")]
    public void NonBooleanOperandIsAWrongArgument(string condition, string message)
    {
        RunSession session = Run($"if {condition}:\n    move(EAST)", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(message, session.ErrorMessage);
    }

    [Theory]
    [InlineData("True and True", "Error on line 1: 'and' is not unlocked yet. Buy \"and, or, not\" in Unlocks for 60 POTATO.")]
    [InlineData("True or True", "Error on line 1: 'or' is not unlocked yet. Buy \"and, or, not\" in Unlocks for 60 POTATO.")]
    [InlineData("not True", "Error on line 1: 'not' is not unlocked yet. Buy \"and, or, not\" in Unlocks for 60 POTATO.")]
    public void LogicIsLockedUntilTheUnlockIsBought(string condition, string message)
    {
        GameProgress progress = new GameProgress(_rules);
        Unlock conditions = _rules.Unlocks.Single(unlock => unlock.Id == "CONDITIONS");
        progress.AddItems(conditions.Price.Item, conditions.Price.Count);
        progress.Buy(conditions);

        RunSession session = RunSession.Start($"if {condition}:\n    move(EAST)", ScriptLanguage.Python, progress);

        Assert.Equal(message, session.ErrorMessage);
    }

    [Fact]
    public void CounterReadOutsideItsLoopInsideALogicCondition()
    {
        RunSession session = Run("for i in range(2):\n    harvest()\nif i == 0 and True:\n    harvest()", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("NameError on line 3: 'i' can only be used inside its for loop", session.ErrorMessage);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 0)]
    public void ElifChainOfThreeRunsOnlyTheMatchingBranch(int value, int moves)
    {
        string source = $"x = {value}\nif x == 1:\n    move(EAST)\nelif x == 2:\n    move(EAST)\n    move(EAST)\nelif x == 3:\n    move(EAST)\n    move(EAST)\n    move(EAST)\n";

        RunSession session = Run(source, AllUnlocked(_rules));

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(_rules.TickCosts.AssignTicks + moves * _rules.TickCosts.MoveTicks, session.TicksUsed);
    }

    private static RunSession Run(string source, GameProgress progress)
    {
        RunSession session = RunSession.Start(source, ScriptLanguage.Python, progress);
        for (int tick = 0; tick < MaxTicks && session.Status == StepStatus.Running; tick++)
        {
            session.StepTick();
        }

        return session;
    }
}
