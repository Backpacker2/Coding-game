using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using CodingGame.Core.Simulation;
using Xunit;
using static CodingGame.Core.Tests.Interpreter.EvaluatorTestSupport;

namespace CodingGame.Core.Tests.Interpreter.Python;

public class PythonVariablesEndToEndTests
{
    private const int MaxTicks = 10_000;
    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    [Fact]
    public void LoopCounterHoldsTheIterationNumberInsideItsLoop()
    {
        GameProgress progress = AllUnlocked(_rules);
        TilePosition start = progress.Field.DronePosition;

        RunSession session = Run("for i in range(3):\n    if i == 1:\n        move(EAST)", progress);

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(new TilePosition(start.X + 1, start.Y), progress.Field.DronePosition);
    }

    [Fact]
    public void VariableCountedUpInAWhileLoopIsUsedAsRangeArgument()
    {
        GameProgress progress = AllUnlocked(_rules);
        TilePosition start = progress.Field.DronePosition;
        const string source = """
        count = 0
        while count < 2:
            count = count + 1
        for i in range(count):
            move(EAST)
        """;

        RunSession session = Run(source, progress);

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(new TilePosition(start.X + 2, start.Y), progress.Field.DronePosition);
    }

    [Theory]
    [InlineData("2 + 3 * 4", 13)]
    [InlineData("(2 + 3) * 4", 19)]
    [InlineData("10 - 2 - 3", 4)]
    [InlineData("2 * 3 + 4", 9)]
    public void ArithmeticFollowsTheUsualPrecedence(string count, int lastCounterValue)
    {
        GameProgress progress = AllUnlocked(_rules);
        TilePosition start = progress.Field.DronePosition;
        string source = $"for i in range({count}):\n    last = i\nif last == {lastCounterValue}:\n    move(EAST)";

        RunSession session = Run(source, progress);

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(new TilePosition(start.X + 1, start.Y), progress.Field.DronePosition);
    }

    [Fact]
    public void UnaryMinusNegatesNumbersAndVariables()
    {
        GameProgress progress = AllUnlocked(_rules);
        TilePosition start = progress.Field.DronePosition;
        const string source = """
        x = 5
        y = -x + 7
        if y == 2:
            move(EAST)
        if -1 * -1 == 1:
            move(EAST)
        """;

        RunSession session = Run(source, progress);

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(new TilePosition(start.X + 2, start.Y), progress.Field.DronePosition);
    }

    [Theory]
    [InlineData("1 < 2", 1)]
    [InlineData("2 < 2", 0)]
    [InlineData("2 > 1", 1)]
    [InlineData("1 > 1", 0)]
    [InlineData("2 <= 2", 1)]
    [InlineData("3 <= 2", 0)]
    [InlineData("2 >= 2", 1)]
    [InlineData("1 >= 2", 0)]
    public void EachOrderingComparisonGivesItsResult(string condition, int expectedMoves)
    {
        GameProgress progress = AllUnlocked(_rules);
        TilePosition start = progress.Field.DronePosition;

        RunSession session = Run($"if {condition}:\n    move(EAST)", progress);

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(new TilePosition(start.X + expectedMoves, start.Y), progress.Field.DronePosition);
    }

    [Fact]
    public void OrderingComparisonOfADirectionIsWrongArgument()
    {
        RunSession session = Run("if EAST < 2:\n    harvest()", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            "TypeError on line 1: < needs two numbers, got a direction and a number",
            session.ErrorMessage);
    }

    [Fact]
    public void AddingADirectionIsWrongArgument()
    {
        RunSession session = Run("x = EAST + 1", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            "TypeError on line 1: + needs two numbers, got a direction and a number",
            session.ErrorMessage);
    }

    [Fact]
    public void NegatingADirectionIsWrongArgument()
    {
        RunSession session = Run("x = -EAST", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("TypeError on line 1: - needs a number, got a direction", session.ErrorMessage);
    }

    [Theory]
    [InlineData("x = 2147483647 + 1")]
    [InlineData("x = 100000 * 100000")]
    public void ResultOutsideTheNumberRangeIsWrongArgument(string source)
    {
        RunSession session = Run(source, AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("TypeError on line 1: number too big for this game", session.ErrorMessage);
    }

    [Theory]
    [InlineData("x = 4 / 2", "/")]
    [InlineData("x = 5 % 2", "%")]
    [InlineData("x = 5 // 2", "//")]
    [InlineData("x = 2 ** 3", "**")]
    public void DivisionAndRemainderAreNotInGame(string source, string operatorText)
    {
        RunSession session = Run(source, AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            $"Error on line 1: '{operatorText}' is not part of this game yet",
            session.ErrorMessage);
    }

    [Fact]
    public void ChainedComparisonIsStillNotInGame()
    {
        RunSession session = Run("if 1 < 2 < 3:\n    harvest()", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("Error on line 1: 'chained comparisons' is not part of this game yet", session.ErrorMessage);
    }

    [Fact]
    public void CounterReadOutsideItsLoopIsANameError()
    {
        RunSession session = Run("for i in range(2):\n    move(EAST)\nmove(i)", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            "NameError on line 3: 'i' can only be used inside its for loop",
            session.ErrorMessage);
    }

    [Fact]
    public void AssigningACounterIsASyntaxError()
    {
        RunSession session = Run("for i in range(2):\n    i = 5", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("SyntaxError on line 2: 'i' is a loop counter and cannot be changed", session.ErrorMessage);
    }

    [Fact]
    public void NestedLoopWithTheSameCounterIsASyntaxError()
    {
        RunSession session = Run("for i in range(2):\n    for i in range(2):\n        harvest()", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            "SyntaxError on line 2: 'i' is already the counter of an outer loop; pick another name",
            session.ErrorMessage);
    }

    [Fact]
    public void VariableNamedLikeAGameNameIsASyntaxError()
    {
        RunSession session = Run("EAST = 3", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("SyntaxError on line 1: 'EAST' is a name the game uses; pick another name", session.ErrorMessage);
    }

    [Fact]
    public void VariableNamedLikeALoopCounterIsASyntaxError()
    {
        RunSession session = Run("for i in range(2):\n    harvest()\ni = 5", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("SyntaxError on line 3: 'i' is already a loop counter; pick another name", session.ErrorMessage);
    }

    [Fact]
    public void VariableNamedLikeAFunctionIsASyntaxError()
    {
        RunSession session = Run("def f():\n    harvest()\nf = 1", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("SyntaxError on line 3: 'f' is already a function; pick another name", session.ErrorMessage);
    }

    [Fact]
    public void ChangingAVariableInsideAFunctionIsNotInGame()
    {
        RunSession session = Run("count = 0\ndef f():\n    count = 1\nf()", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            "Error on line 3: 'changing a variable inside a function' is not part of this game yet",
            session.ErrorMessage);
    }

    [Fact]
    public void FunctionReadsATopLevelVariable()
    {
        GameProgress progress = AllUnlocked(_rules);
        TilePosition start = progress.Field.DronePosition;
        const string source = """
        count = 2
        def f():
            for i in range(count):
                move(EAST)
        f()
        """;

        RunSession session = Run(source, progress);

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(new TilePosition(start.X + 2, start.Y), progress.Field.DronePosition);
    }

    [Fact]
    public void LoopCounterOfACallerIsNotChangedByALoopInTheFunction()
    {
        GameProgress progress = AllUnlocked(_rules);
        TilePosition start = progress.Field.DronePosition;
        const string source = """
        def f():
            for i in range(3):
                pass
        for i in range(2):
            f()
            if i == 1:
                move(EAST)
        """;

        RunSession session = Run(source, progress);

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(new TilePosition(start.X + 1, start.Y), progress.Field.DronePosition);
    }

    [Fact]
    public void ReadingAVariableBeforeItsAssignmentIsANameError()
    {
        RunSession session = Run("y = count + 1", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("NameError on line 1: name 'count' is not defined", session.ErrorMessage);
    }

    [Fact]
    public void VariablesWithNothingBoughtNameTheUnlock()
    {
        RunSession session = Run("x = 1", new GameProgress(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            "Error on line 1: 'variables' is not unlocked yet. Buy \"Variables and counting\" in Unlocks for 40 POTATO.",
            session.ErrorMessage);
    }

    [Fact]
    public void AssignmentCostsAssignTicks()
    {
        RunSession withAssignment = Run("x = 1\nmove(EAST)", AllUnlocked(_rules));
        RunSession withoutAssignment = Run("move(EAST)", AllUnlocked(_rules));

        Assert.Equal(withoutAssignment.TicksUsed + _rules.TickCosts.AssignTicks, withAssignment.TicksUsed);
    }

    [Fact]
    public void ArithmeticAndComparisonCostNoExtraTicks()
    {
        RunSession arithmetic = Run("x = 1 + 2 * 3", AllUnlocked(_rules));
        RunSession constant = Run("x = 7", AllUnlocked(_rules));
        RunSession comparison = Run("if 1 < 2:\n    harvest()", AllUnlocked(_rules));
        RunSession equality = Run("if 1 == 1:\n    harvest()", AllUnlocked(_rules));

        Assert.Equal(constant.TicksUsed, arithmetic.TicksUsed);
        Assert.Equal(equality.TicksUsed, comparison.TicksUsed);
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
