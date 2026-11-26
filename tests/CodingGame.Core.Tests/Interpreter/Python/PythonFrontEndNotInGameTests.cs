using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.Python;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter.Python;

public class PythonFrontEndNotInGameTests
{
    [Theory]
    [InlineData("import os", 1, "import")]
    [InlineData("from a import b", 1, "from")]
    [InlineData("return", 1, "return")]
    [InlineData("x += 1", 1, "+=")]
    [InlineData("a / 2", 1, "/")]
    [InlineData("a % 2", 1, "%")]
    [InlineData("a // 2", 1, "//")]
    [InlineData("a ** 2", 1, "**")]
    [InlineData("move(a / 2)", 1, "/")]
    [InlineData("if a and b:\n    pass", 1, "and")]
    [InlineData("if not ripe():\n    pass", 1, "not")]
    [InlineData("while None:\n    pass", 1, "None")]
    [InlineData("lst[0]", 1, "[")]
    [InlineData("a.b()", 1, ".")]
    [InlineData("a; b", 1, ";")]
    [InlineData("lambda", 1, "lambda")]
    [InlineData("class A:", 1, "class")]
    [InlineData("try:", 1, "try")]
    [InlineData("break", 1, "break")]
    [InlineData("continue", 1, "continue")]
    [InlineData("harvest()\nimport os", 2, "import")]
    public void ReportsSyntaxThatIsNotInTheGameYet(string source, int line, string detail)
    {
        ParseResult result = PythonFrontEnd.Parse(source);

        Assert.Null(result.Program);
        Assert.Equal(new ScriptError(ScriptErrorKind.NotInGame, line, detail), result.Error);
    }

    [Theory]
    [InlineData("a == b == c", 1, "chained comparisons")]
    [InlineData("a != b == c", 1, "chained comparisons")]
    public void ReportsChainedComparisonsAsNotInGame(string source, int line, string detail)
    {
        ParseResult result = PythonFrontEnd.Parse(source);

        Assert.Null(result.Program);
        Assert.Equal(new ScriptError(ScriptErrorKind.NotInGame, line, detail), result.Error);
    }

    [Theory]
    [InlineData("harvest())", 1, "unmatched ')'")]
    [InlineData(")", 1, "unmatched ')'")]
    [InlineData("harvest(", 1, "'(' was never closed")]
    [InlineData("move(EAST\nharvest()", 1, "'(' was never closed")]
    [InlineData("f(g(1)", 1, "'(' was never closed")]
    [InlineData("f(1\n ", 1, "'(' was never closed")]
    public void ReportsBracketsThatAreNotClosedOrOpened(string source, int line, string detail)
    {
        ParseResult result = PythonFrontEnd.Parse(source);

        Assert.Null(result.Program);
        Assert.Equal(new ScriptError(ScriptErrorKind.SyntaxError, line, detail), result.Error);
    }

    [Fact]
    public void ParsesPrintAsAnOrdinaryCall()
    {
        ParseResult result = PythonFrontEnd.Parse("print(x)");

        Assert.Null(result.Error);
        Assert.Equal("1: print(x)", TreeDump.Of(result.Program!));
    }

    [Fact]
    public void FormatsTheNotInGameMessageForPython()
    {
        ParseResult result = PythonFrontEnd.Parse("import os");

        Assert.Equal("Error on line 1: 'import' is not part of this game yet", result.Error!.ToMessage(ScriptLanguage.Python));
    }

    [Fact]
    public void FormatsTheNeverClosedMessageForPython()
    {
        ParseResult result = PythonFrontEnd.Parse("harvest(");

        Assert.Equal("SyntaxError on line 1: '(' was never closed", result.Error!.ToMessage(ScriptLanguage.Python));
    }

    [Fact]
    public void ReportsDeepNestingAsAnErrorInsteadOfCrashing()
    {
        string calls = string.Concat(Enumerable.Repeat("f(", 100_000)) + new string(')', 100_000);
        string blocks = string.Concat(Enumerable.Range(0, 101).Select(depth => new string(' ', depth) + "if True:\n"))
            + new string(' ', 101) + "pass";

        Assert.Equal(new ScriptError(ScriptErrorKind.SyntaxError, 1, "too many nested levels"), PythonFrontEnd.Parse(calls).Error);
        Assert.Equal(new ScriptError(ScriptErrorKind.SyntaxError, 102, "too many nested levels"), PythonFrontEnd.Parse(blocks).Error);
    }

    [Fact]
    public void ReportsALongElifChainAsAnErrorInsteadOfCrashing()
    {
        string chain = "if a:\n    pass\n" + string.Concat(Enumerable.Repeat("elif a:\n    pass\n", 100_000));

        Assert.Equal(new ScriptError(ScriptErrorKind.SyntaxError, 202, "too many nested levels"), PythonFrontEnd.Parse(chain).Error);
    }

    [Fact]
    public void AcceptsNestingUpToTheLimit()
    {
        string calls = string.Concat(Enumerable.Repeat("f(", 100)) + new string(')', 100);

        Assert.Null(PythonFrontEnd.Parse(calls).Error);
    }
}
