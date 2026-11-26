using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.JavaScript;
using CodingGame.Core.Interpreter.Python;
using CodingGame.Core.Rules;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter;

public class UnlockCheckTests
{
    private const string AllFeaturesPython = """
        harvest()
        for i in range(3):
            if ground() == SOIL:
                plant(RADISH)
        while ripe() != False:
            move(NORTH)
        def f():
            harvest()
        """;

    private const string AllFeaturesJavaScript = """
        harvest();
        for (let i = 0; i < 3; i++) {
            if (ground() === SOIL) {
                plant(RADISH);
            }
        }
        while (ripe() !== false) {
            move(NORTH);
        }
        function f() {
            harvest();
        }
        """;

    [Fact]
    public void NothingLockedGivesNoError()
    {
        Assert.Null(UnlockCheck.FindLocked(PythonProgram(AllFeaturesPython), Locked()));
        Assert.Null(UnlockCheck.FindLocked(JavaScriptProgram(AllFeaturesJavaScript), Locked()));
    }

    [Fact]
    public void LockedForLoopIsReportedAtItsLine()
    {
        string source = "harvest()\nfor i in range(3):\n    harvest()";
        var locked = Locked("for");
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 2, "for", locked["for"]),
            UnlockCheck.FindLocked(PythonProgram(source), locked));
    }

    [Fact]
    public void LockedForLoopInJavaScriptIsReported()
    {
        string source = "for (let i = 0; i < 3; i++) {\n harvest();\n}";
        var locked = Locked("for");
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "for", locked["for"]),
            UnlockCheck.FindLocked(JavaScriptProgram(source), locked));
    }

    [Fact]
    public void LockedWhileLoopIsReportedInBothLanguages()
    {
        var locked = Locked("while");
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "while", locked["while"]),
            UnlockCheck.FindLocked(PythonProgram("while True:\n    harvest()"), locked));
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "while", locked["while"]),
            UnlockCheck.FindLocked(JavaScriptProgram("while (true) { harvest(); }"), locked));
    }

    [Fact]
    public void LockedIfIsReportedAndTheFirstIfWinsOverElif()
    {
        var locked = Locked("if");
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "if", locked["if"]),
            UnlockCheck.FindLocked(PythonProgram("if ripe():\n    harvest()"), locked));
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "if", locked["if"]),
            UnlockCheck.FindLocked(
                PythonProgram("if ripe():\n    harvest()\nelif ripe():\n    harvest()"),
                locked));
    }

    [Fact]
    public void LockedFunctionDefinitionUsesTheWordOfEachLanguage()
    {
        var locked = Locked("def");
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "def", locked["def"]),
            UnlockCheck.FindLocked(PythonProgram("def f():\n    harvest()"), locked));
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "function", locked["def"]),
            UnlockCheck.FindLocked(JavaScriptProgram("function f() { harvest(); }"), locked));
    }

    [Fact]
    public void LockedNameIsReportedInConditionAndInsideABody()
    {
        var locked = Locked("ripe");
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "ripe", locked["ripe"]),
            UnlockCheck.FindLocked(PythonProgram("if ripe():\n    harvest()"), locked));
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 2, "ripe", locked["ripe"]),
            UnlockCheck.FindLocked(
                PythonProgram("for i in range(2):\n    if ripe():\n        harvest()"),
                locked));
    }

    [Fact]
    public void CallIsCheckedBeforeItsArguments()
    {
        var locked = Locked("plant", "RADISH");
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "plant", locked["plant"]),
            UnlockCheck.FindLocked(PythonProgram("plant(RADISH)"), locked));
        var radishOnly = Locked("RADISH");
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "RADISH", radishOnly["RADISH"]),
            UnlockCheck.FindLocked(PythonProgram("plant(RADISH)"), radishOnly));
    }

    [Fact]
    public void ComparisonUsesTheOperatorOfEachLanguage()
    {
        var locked = Locked("==");
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "==", locked["=="]),
            UnlockCheck.FindLocked(PythonProgram("if ground() == SOIL:\n    harvest()"), locked));
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "===", locked["=="]),
            UnlockCheck.FindLocked(JavaScriptProgram("if (ground() === SOIL) {\n harvest();\n}"), locked));
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "!=", locked["=="]),
            UnlockCheck.FindLocked(PythonProgram("if ground() != SOIL:\n    harvest()"), locked));
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "!==", locked["=="]),
            UnlockCheck.FindLocked(JavaScriptProgram("if (ground() !== SOIL) {\n harvest();\n}"), locked));
    }

    [Fact]
    public void FirstLockedUseInSourceOrderWins()
    {
        string source = "while True:\n    for i in range(2):\n        harvest()";
        var locked = Locked("while", "for");
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "while", locked["while"]),
            UnlockCheck.FindLocked(PythonProgram(source), locked));
    }

    [Fact]
    public void UnlockedFeaturesNeverGiveAnError()
    {
        string source = "move(EAST)\nharvest()\nif True:\n    harvest()";
        Assert.Null(UnlockCheck.FindLocked(PythonProgram(source), Locked("plant", "ripe", "==")));
    }

    [Fact]
    public void ReadingOrChangingAVariableNeedsTheVariablesFeature()
    {
        var locked = Locked("variables");
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "variables", locked["variables"]),
            UnlockCheck.FindLocked(PythonProgram("count = 1"), locked));
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 2, "variables", locked["variables"]),
            UnlockCheck.FindLocked(PythonProgram("for i in range(2):\n    move(i)"), locked));
    }

    [Fact]
    public void LoopCounterThatIsNeverReadNeedsOnlyTheForFeature()
    {
        Assert.Null(UnlockCheck.FindLocked(PythonProgram("for i in range(2):\n    harvest()"), Locked("variables")));
    }

    [Theory]
    [InlineData("x = 1 + 2", "+")]
    [InlineData("x = 1 - 2", "-")]
    [InlineData("x = 1 * 2", "*")]
    [InlineData("x = -2", "-")]
    [InlineData("if 1 < 2:\n    pass", "<")]
    [InlineData("if 1 >= 2:\n    pass", ">=")]
    public void ArithmeticAndOrderingNeedTheirOwnSymbolAsFeature(string source, string feature)
    {
        var locked = Locked(feature);
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, feature, locked[feature]),
            UnlockCheck.FindLocked(PythonProgram(source), locked));
    }

    private static Dictionary<string, Unlock> Locked(params string[] features)
    {
        return features.ToDictionary(
            feature => feature,
            feature => new Unlock(feature, feature, new UnlockPrice("CLOVER", 10), [feature], null));
    }

    private static ScriptProgram PythonProgram(string source)
    {
        return PythonFrontEnd.Parse(source).Program!;
    }

    private static ScriptProgram JavaScriptProgram(string source)
    {
        return JavaScriptFrontEnd.Parse(source).Program!;
    }
}
