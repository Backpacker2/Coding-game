using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using CodingGame.Core.Simulation;
using Xunit;
using static CodingGame.Core.Tests.Interpreter.EvaluatorTestSupport;

namespace CodingGame.Core.Tests.Interpreter.JavaScript;

public class JavaScriptVariablesEndToEndTests
{
    private const int MaxTicks = 10_000;
    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    [Fact]
    public void LoopCounterHoldsTheIterationNumberInsideItsLoop()
    {
        GameProgress progress = AllUnlocked(_rules);
        TilePosition start = progress.Field.DronePosition;

        RunSession session = Run("for (let i = 0; i < 3; i++) {\n    if (i === 1) {\n        move(EAST);\n    }\n}", progress);

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(new TilePosition(start.X + 1, start.Y), progress.Field.DronePosition);
    }

    [Fact]
    public void VariableCountedUpInAWhileLoopIsUsedAsLoopCount()
    {
        GameProgress progress = AllUnlocked(_rules);
        TilePosition start = progress.Field.DronePosition;
        const string source = """
        let count = 0;
        while (count < 2) {
            count = count + 1;
        }
        for (let i = 0; i < count; i++) {
            move(EAST);
        }
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
        string source = $"let last = 0;\nfor (let i = 0; i < {count}; i++) {{\n    last = i;\n}}\nif (last === {lastCounterValue}) {{\n    move(EAST);\n}}";

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
        let x = 5;
        let y = -x + 7;
        if (y === 2) {
            move(EAST);
        }
        if (-1 * -1 === 1) {
            move(EAST);
        }
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

        RunSession session = Run($"if ({condition}) {{\n    move(EAST);\n}}", progress);

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(new TilePosition(start.X + expectedMoves, start.Y), progress.Field.DronePosition);
    }

    [Fact]
    public void OrderingComparisonOfADirectionIsWrongArgument()
    {
        RunSession session = Run("if (EAST < 2) {\n    harvest();\n}", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            "TypeError on line 1: < needs two numbers, got a direction and a number",
            session.ErrorMessage);
    }

    [Fact]
    public void AddingADirectionIsWrongArgument()
    {
        RunSession session = Run("let x = EAST + 1;", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            "TypeError on line 1: + needs two numbers, got a direction and a number",
            session.ErrorMessage);
    }

    [Fact]
    public void NegatingADirectionIsWrongArgument()
    {
        RunSession session = Run("let x = -EAST;", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("TypeError on line 1: - needs a number, got a direction", session.ErrorMessage);
    }

    [Theory]
    [InlineData("let x = 2147483647 + 1;")]
    [InlineData("let x = 100000 * 100000;")]
    public void ResultOutsideTheNumberRangeIsWrongArgument(string source)
    {
        RunSession session = Run(source, AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("TypeError on line 1: number too big for this game", session.ErrorMessage);
    }

    [Theory]
    [InlineData("let x = 4 / 2;", "/")]
    [InlineData("let x = 5 % 2;", "%")]
    [InlineData("let x = 2 ** 3;", "**")]
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
        RunSession session = Run("if (1 < 2 < 3) {\n    harvest();\n}", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("Error on line 1: 'chained comparisons' is not part of this game yet", session.ErrorMessage);
    }

    [Fact]
    public void CounterReadOutsideItsLoopIsAReferenceError()
    {
        RunSession session = Run("for (let i = 0; i < 2; i++) {\n    move(EAST);\n}\nmove(i);", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            "ReferenceError on line 4: 'i' can only be used inside its for loop",
            session.ErrorMessage);
    }

    [Fact]
    public void AssigningACounterIsASyntaxError()
    {
        RunSession session = Run("for (let i = 0; i < 2; i++) {\n    i = 5;\n}", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("SyntaxError on line 2: 'i' is a loop counter and cannot be changed", session.ErrorMessage);
    }

    [Fact]
    public void NestedLoopWithTheSameCounterIsASyntaxError()
    {
        RunSession session = Run(
            "for (let i = 0; i < 2; i++) {\n    for (let i = 0; i < 2; i++) {\n        harvest();\n    }\n}",
            AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            "SyntaxError on line 2: 'i' is already the counter of an outer loop; pick another name",
            session.ErrorMessage);
    }

    [Fact]
    public void VariableNamedLikeAGameNameIsASyntaxError()
    {
        RunSession session = Run("let EAST = 3;", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("SyntaxError on line 1: 'EAST' is a name the game uses; pick another name", session.ErrorMessage);
    }

    [Fact]
    public void VariableNamedLikeALoopCounterIsASyntaxError()
    {
        RunSession session = Run("for (let i = 0; i < 2; i++) {\n    harvest();\n}\ni = 5;", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("SyntaxError on line 4: 'i' is already a loop counter; pick another name", session.ErrorMessage);
    }

    [Fact]
    public void VariableNamedLikeAFunctionIsASyntaxError()
    {
        RunSession session = Run("function f() {\n    harvest();\n}\nlet f = 1;", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("SyntaxError on line 4: 'f' is already a function; pick another name", session.ErrorMessage);
    }

    [Fact]
    public void ChangingAVariableInsideAFunctionIsNotInGame()
    {
        RunSession session = Run("let count = 0;\nfunction f() {\n    count = 1;\n}\nf();", AllUnlocked(_rules));

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
        let count = 2;
        function f() {
            for (let i = 0; i < count; i++) {
                move(EAST);
            }
        }
        f();
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
        function f() {
            for (let i = 0; i < 3; i++) {}
        }
        for (let i = 0; i < 2; i++) {
            f();
            if (i === 1) {
                move(EAST);
            }
        }
        """;

        RunSession session = Run(source, progress);

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(new TilePosition(start.X + 1, start.Y), progress.Field.DronePosition);
    }

    [Fact]
    public void ReadingAVariableBeforeItsDeclarationIsAReferenceError()
    {
        RunSession session = Run("let y = count + 1;", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("ReferenceError on line 1: count is not defined", session.ErrorMessage);
    }

    [Fact]
    public void VariablesWithNothingBoughtNameTheUnlock()
    {
        RunSession session = Run("let x = 1;", new GameProgress(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            "Error on line 1: 'variables' is not unlocked yet. Buy \"Variables and counting\" in Unlocks for 40 POTATO.",
            session.ErrorMessage);
    }

    [Fact]
    public void AssignmentCostsAssignTicks()
    {
        RunSession withAssignment = Run("let x = 1;\nmove(EAST);", AllUnlocked(_rules));
        RunSession withoutAssignment = Run("move(EAST);", AllUnlocked(_rules));

        Assert.Equal(withoutAssignment.TicksUsed + _rules.TickCosts.AssignTicks, withAssignment.TicksUsed);
    }

    [Fact]
    public void ArithmeticAndComparisonCostNoExtraTicks()
    {
        RunSession arithmetic = Run("let x = 1 + 2 * 3;", AllUnlocked(_rules));
        RunSession constant = Run("let x = 7;", AllUnlocked(_rules));
        RunSession comparison = Run("if (1 < 2) {\n    harvest();\n}", AllUnlocked(_rules));
        RunSession equality = Run("if (1 === 1) {\n    harvest();\n}", AllUnlocked(_rules));

        Assert.Equal(constant.TicksUsed, arithmetic.TicksUsed);
        Assert.Equal(equality.TicksUsed, comparison.TicksUsed);
    }

    [Fact]
    public void LetInsideABlockIsNotInGame()
    {
        RunSession session = Run("if (true) {\n    let x = 1;\n}", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("Error on line 2: 'let inside a block' is not part of this game yet", session.ErrorMessage);
    }

    [Fact]
    public void DeclaringTheSameNameTwiceIsASyntaxError()
    {
        RunSession session = Run("let x = 1;\nlet x = 2;", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("SyntaxError on line 2: Identifier 'x' has already been declared", session.ErrorMessage);
    }

    [Fact]
    public void AssigningAnUndeclaredNameIsAReferenceError()
    {
        RunSession session = Run("x = 1;", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            "ReferenceError on line 1: 'x' is not declared; write let x = ... first",
            session.ErrorMessage);
    }

    [Fact]
    public void AssigningAVariableDeclaredLaterIsAReferenceError()
    {
        RunSession session = Run("x = 1;\nlet x = 2;", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            "ReferenceError on line 1: 'x' is not declared; write let x = ... first",
            session.ErrorMessage);
    }

    [Fact]
    public void AssigningADeclaredVariableInsideABlockChangesIt()
    {
        GameProgress progress = AllUnlocked(_rules);
        TilePosition start = progress.Field.DronePosition;
        const string source = """
        let steps = 0;
        if (true) {
            steps = steps + 2;
        }
        for (let i = 0; i < steps; i++) {
            move(EAST);
        }
        """;

        RunSession session = Run(source, progress);

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Equal(new TilePosition(start.X + 2, start.Y), progress.Field.DronePosition);
    }

    [Theory]
    [InlineData("var x = 1;", "var")]
    [InlineData("const x = 1;", "const")]
    public void VarAndConstAreNotInGame(string source, string keyword)
    {
        RunSession session = Run(source, AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal($"Error on line 1: '{keyword}' is not part of this game yet", session.ErrorMessage);
    }

    [Fact]
    public void AssignmentInAConditionIsASyntaxErrorThatNamesTheStrictOperator()
    {
        RunSession session = Run("let x = 3;\nif (x = 3) {\n    harvest();\n}", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            "SyntaxError on line 2: use === to compare; = gives a variable a value",
            session.ErrorMessage);
    }

    [Fact]
    public void AssignmentInAWhileConditionIsASyntaxErrorThatNamesTheStrictOperator()
    {
        RunSession session = Run("let x = 3;\nwhile (x = 3) {\n    harvest();\n}", AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            "SyntaxError on line 2: use === to compare; = gives a variable a value",
            session.ErrorMessage);
    }

    [Theory]
    [InlineData("let count = 0;\ncount += 1;", 2, "+=")]
    [InlineData("let count = 0;\ncount -= 1;", 2, "-=")]
    [InlineData("let count = 0;\ncount *= 2;", 2, "*=")]
    [InlineData("let count = 0;\ncount++;", 2, "++")]
    [InlineData("let count = 0;\ncount--;", 2, "--")]
    public void CompoundAssignmentOutsideTheForHeaderIsNotInGame(string source, int line, string operatorText)
    {
        RunSession session = Run(source, AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal(
            $"Error on line {line}: '{operatorText}' is not part of this game yet",
            session.ErrorMessage);
    }

    [Theory]
    [InlineData("let range = 3;")]
    [InlineData("for (let range = 0; range < 2; range++) {\n    harvest();\n}")]
    public void RangeIsAGameNameThatCannotBeAVariableOrCounter(string source)
    {
        RunSession session = Run(source, AllUnlocked(_rules));

        Assert.Equal(StepStatus.Failed, session.Status);
        Assert.Equal("SyntaxError on line 1: 'range' is a name the game uses; pick another name", session.ErrorMessage);
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
