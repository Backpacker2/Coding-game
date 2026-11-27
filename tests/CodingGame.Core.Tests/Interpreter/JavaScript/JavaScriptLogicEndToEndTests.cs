using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using CodingGame.Core.Simulation;
using Xunit;
using static CodingGame.Core.Tests.Interpreter.EvaluatorTestSupport;

namespace CodingGame.Core.Tests.Interpreter.JavaScript;

public class JavaScriptLogicEndToEndTests
{
    private const int MaxTicks = 10_000;
    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    [Theory]
    [InlineData("true && true", true)]
    [InlineData("true && false", false)]
    [InlineData("false && true", false)]
    [InlineData("false && false", false)]
    [InlineData("true || true", true)]
    [InlineData("true || false", true)]
    [InlineData("false || true", true)]
    [InlineData("false || false", false)]
    [InlineData("!true", false)]
    [InlineData("!false", true)]
    [InlineData("!(1 === 2) && 3 === 3", true)]
    [InlineData("!(1 === 1) || 2 === 2", true)]
    [InlineData("true || false && false", true)]
    [InlineData("1 === 1 && 2 === 3 || 4 === 4", true)]
    public void MovesOnlyWhenTheConditionHolds(string condition, bool holds)
    {
        GameProgress progress = AllUnlocked(_rules);
        TilePosition start = progress.Field.DronePosition;

        RunSession session = Run($"if ({condition}) {{\n    move(EAST);\n}}", progress);

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(holds ? new TilePosition(start.X + 1, start.Y) : start, progress.Field.DronePosition);
    }

    [Theory]
    [InlineData("if (false && ripe()) {\n    move(EAST);\n}", "if (false && true) {\n    move(EAST);\n}")]
    [InlineData("if (true || ripe()) {\n    move(EAST);\n}", "if (true || true) {\n    move(EAST);\n}")]
    public void RightSideIsSkippedWhenTheLeftSideDecides(string skipped, string reference)
    {
        RunSession withRipe = Run(skipped, AllUnlocked(_rules));
        RunSession withoutRipe = Run(reference, AllUnlocked(_rules));

        Assert.Equal(withoutRipe.TicksUsed, withRipe.TicksUsed);
    }

    [Fact]
    public void RightSideCostsItsTicksWhenItIsEvaluated()
    {
        RunSession withRipe = Run("if (true && ripe()) {\n    move(EAST);\n}", AllUnlocked(_rules));
        RunSession withoutRipe = Run("if (true && true) {\n    move(EAST);\n}", AllUnlocked(_rules));

        Assert.Equal(withoutRipe.TicksUsed + _rules.TickCosts.RipeTicks, withRipe.TicksUsed);
    }

    [Theory]
    [InlineData("1 && true", "TypeError on line 1: && needs true or false on both sides, got a number")]
    [InlineData("true && 1", "TypeError on line 1: && needs true or false on both sides, got a number")]
    [InlineData("false || EAST", "TypeError on line 1: || needs true or false on both sides, got a direction")]
    [InlineData("!3", "TypeError on line 1: ! needs true or false, got a number")]
    [InlineData("!EAST", "TypeError on line 1: ! needs true or false, got a direction")]
    public void NonBooleanOperandIsAWrongArgument(string condition, string message)
    {
        RunSession session = Run($"if ({condition}) {{\n    move(EAST);\n}}", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(message, session.ErrorMessage);
    }

    [Theory]
    [InlineData("true && true", "Error on line 1: '&&' is not unlocked yet. Buy \"and, or, not\" in Unlocks for 60 POTATO.")]
    [InlineData("true || true", "Error on line 1: '||' is not unlocked yet. Buy \"and, or, not\" in Unlocks for 60 POTATO.")]
    [InlineData("!true", "Error on line 1: '!' is not unlocked yet. Buy \"and, or, not\" in Unlocks for 60 POTATO.")]
    public void LogicIsLockedUntilTheUnlockIsBought(string condition, string message)
    {
        GameProgress progress = new GameProgress(_rules);
        Unlock conditions = _rules.Unlocks.Single(unlock => unlock.Id == "CONDITIONS");
        progress.AddItems(conditions.Price.Item, conditions.Price.Count);
        progress.Buy(conditions);

        RunSession session = RunSession.Start($"if ({condition}) {{\n    move(EAST);\n}}", ScriptLanguage.JavaScript, progress);

        Assert.Equal(message, session.ErrorMessage);
    }

    [Fact]
    public void CounterReadOutsideItsLoopInsideALogicCondition()
    {
        RunSession session = Run("for (let i = 0; i < 2; i++) {\n    harvest();\n}\nif (i === 0 && true) {\n    harvest();\n}", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("ReferenceError on line 4: 'i' can only be used inside its for loop", session.ErrorMessage);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 0)]
    public void ElseIfChainOfThreeRunsOnlyTheMatchingBranch(int value, int moves)
    {
        string source = $"let x = {value};\n"
            + "if (x === 1) {\n    move(EAST);\n}\n"
            + "else if (x === 2) {\n    move(EAST);\n    move(EAST);\n}\n"
            + "else if (x === 3) {\n    move(EAST);\n    move(EAST);\n    move(EAST);\n}\n";

        RunSession session = Run(source, AllUnlocked(_rules));

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(_rules.TickCosts.AssignTicks + moves * _rules.TickCosts.MoveTicks, session.TicksUsed);
    }

    private static RunSession Run(string source, GameProgress progress)
    {
        RunSession session = RunSession.Start(source, ScriptLanguage.JavaScript, progress);
        for (int tick = 0; tick < MaxTicks && session.Status == StepStatus.Running; tick++)
        {
            session.StepTick();
        }

        return session;
    }
}
