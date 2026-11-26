using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.JavaScript;
using CodingGame.Core.Interpreter.Python;
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
        Assert.Null(UnlockCheck.FindLocked(PythonProgram(AllFeaturesPython), new HashSet<string>()));
        Assert.Null(UnlockCheck.FindLocked(JavaScriptProgram(AllFeaturesJavaScript), new HashSet<string>()));
    }

    [Fact]
    public void LockedForLoopIsReportedAtItsLine()
    {
        string source = "harvest()\nfor i in range(3):\n    harvest()";
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 2, "for"),
            UnlockCheck.FindLocked(PythonProgram(source), new HashSet<string> { "for" }));
    }

    [Fact]
    public void LockedForLoopInJavaScriptIsReported()
    {
        string source = "for (let i = 0; i < 3; i++) {\n harvest();\n}";
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "for"),
            UnlockCheck.FindLocked(JavaScriptProgram(source), new HashSet<string> { "for" }));
    }

    [Fact]
    public void LockedWhileLoopIsReportedInBothLanguages()
    {
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "while"),
            UnlockCheck.FindLocked(PythonProgram("while True:\n    harvest()"), new HashSet<string> { "while" }));
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "while"),
            UnlockCheck.FindLocked(JavaScriptProgram("while (true) { harvest(); }"), new HashSet<string> { "while" }));
    }

    [Fact]
    public void LockedIfIsReportedAndTheFirstIfWinsOverElif()
    {
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "if"),
            UnlockCheck.FindLocked(PythonProgram("if ripe():\n    harvest()"), new HashSet<string> { "if" }));
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "if"),
            UnlockCheck.FindLocked(
                PythonProgram("if ripe():\n    harvest()\nelif ripe():\n    harvest()"),
                new HashSet<string> { "if" }));
    }

    [Fact]
    public void LockedFunctionDefinitionUsesTheWordOfEachLanguage()
    {
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "def"),
            UnlockCheck.FindLocked(PythonProgram("def f():\n    harvest()"), new HashSet<string> { "def" }));
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "function"),
            UnlockCheck.FindLocked(JavaScriptProgram("function f() { harvest(); }"), new HashSet<string> { "def" }));
    }

    [Fact]
    public void LockedNameIsReportedInConditionAndInsideABody()
    {
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "ripe"),
            UnlockCheck.FindLocked(PythonProgram("if ripe():\n    harvest()"), new HashSet<string> { "ripe" }));
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 2, "ripe"),
            UnlockCheck.FindLocked(
                PythonProgram("for i in range(2):\n    if ripe():\n        harvest()"),
                new HashSet<string> { "ripe" }));
    }

    [Fact]
    public void CallIsCheckedBeforeItsArguments()
    {
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "plant"),
            UnlockCheck.FindLocked(PythonProgram("plant(RADISH)"), new HashSet<string> { "plant", "RADISH" }));
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "RADISH"),
            UnlockCheck.FindLocked(PythonProgram("plant(RADISH)"), new HashSet<string> { "RADISH" }));
    }

    [Fact]
    public void ComparisonUsesTheOperatorOfEachLanguage()
    {
        HashSet<string> locked = new() { "==" };
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "=="),
            UnlockCheck.FindLocked(PythonProgram("if ground() == SOIL:\n    harvest()"), locked));
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "==="),
            UnlockCheck.FindLocked(JavaScriptProgram("if (ground() === SOIL) {\n harvest();\n}"), locked));
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "!="),
            UnlockCheck.FindLocked(PythonProgram("if ground() != SOIL:\n    harvest()"), locked));
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "!=="),
            UnlockCheck.FindLocked(JavaScriptProgram("if (ground() !== SOIL) {\n harvest();\n}"), locked));
    }

    [Fact]
    public void FirstLockedUseInSourceOrderWins()
    {
        string source = "while True:\n    for i in range(2):\n        harvest()";
        Assert.Equal(
            new ScriptError(ScriptErrorKind.NotUnlocked, 1, "while"),
            UnlockCheck.FindLocked(PythonProgram(source), new HashSet<string> { "while", "for" }));
    }

    [Fact]
    public void UnlockedFeaturesNeverGiveAnError()
    {
        string source = "move(EAST)\nharvest()\nif True:\n    harvest()";
        Assert.Null(UnlockCheck.FindLocked(PythonProgram(source), new HashSet<string> { "plant", "ripe", "==" }));
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
