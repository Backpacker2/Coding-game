using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.Python;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter.Python;

public class PythonFrontEndStatementTests
{
    [Theory]
    [InlineData("harvest()", "1: harvest()")]
    [InlineData("move(EAST)", "1: move(EAST)")]
    [InlineData("# start\nharvest()\n\nmove(NORTH)  # go\n", "2: harvest()\n4: move(NORTH)")]
    [InlineData("pass", "")]
    [InlineData("pass\nharvest()", "2: harvest()")]
    [InlineData("plant(RADISH)\nf(1, True, False)", "1: plant(RADISH)\n2: f(1, true, false)")]
    [InlineData("ground() == SOIL", "1: (ground() == SOIL)")]
    [InlineData("ripe() != False", "1: (ripe() != false)")]
    [InlineData("f(g(1), 2)", "1: f(g(1), 2)")]
    [InlineData("NORTH", "1: NORTH")]
    public void ParsesStatementsIntoTheExpectedTree(string source, string expectedDump)
    {
        ParseResult result = PythonFrontEnd.Parse(source);

        Assert.Null(result.Error);
        Assert.Equal(expectedDump, TreeDump.Of(result.Program!));
    }

    [Fact]
    public void EmptySourceGivesAnEmptyPythonProgram()
    {
        ParseResult result = PythonFrontEnd.Parse(string.Empty);

        Assert.Null(result.Error);
        Assert.Equal(ScriptLanguage.Python, result.Program!.Language);
        Assert.Empty(result.Program.Body);
    }

    [Fact]
    public void StatementLineIsTheLineOfItsFirstToken()
    {
        ParseResult result = PythonFrontEnd.Parse("\n\nmove(EAST)");

        var statement = Assert.IsType<ExpressionStatement>(Assert.Single(result.Program!.Body));
        var call = Assert.IsType<Call>(statement.Expression);
        Assert.Equal(3, statement.Line);
        Assert.Equal(3, call.Line);
        Assert.Equal(3, call.Arguments[0].Line);
    }

    [Fact]
    public void CompareLineIsTheLineOfItsLeftOperand()
    {
        ParseResult result = PythonFrontEnd.Parse("\nground() == SOIL");

        var statement = Assert.IsType<ExpressionStatement>(Assert.Single(result.Program!.Body));
        var compare = Assert.IsType<Compare>(statement.Expression);
        Assert.Equal(2, compare.Line);
    }

    [Theory]
    [InlineData("move(EAST) harvest()", 1)]
    [InlineData("move(,)", 1)]
    [InlineData("==", 1)]
    [InlineData("ripe() ==", 1)]
    [InlineData("if", 1)]
    [InlineData("harvest()\nmove(EAST EAST)", 2)]
    public void ReportsInvalidSyntaxOnTheLineOfTheFirstUnexpectedToken(string source, int line)
    {
        ParseResult result = PythonFrontEnd.Parse(source);

        Assert.Null(result.Program);
        Assert.Equal(new ScriptError(ScriptErrorKind.SyntaxError, line, "invalid syntax"), result.Error);
    }

    [Fact]
    public void ReportsTheFirstErrorInSourceOrderWhenTheTokenizerAlsoFails()
    {
        ParseResult result = PythonFrontEnd.Parse("move(EAST) harvest()\nmove($)");

        Assert.Null(result.Program);
        Assert.Equal(new ScriptError(ScriptErrorKind.SyntaxError, 1, "invalid syntax"), result.Error);
    }

    [Fact]
    public void ReportsTheTokenizerErrorWhenTheParserReachesTheProblem()
    {
        ParseResult result = PythonFrontEnd.Parse("harvest()\nmove($)");

        Assert.Null(result.Program);
        Assert.Equal(new ScriptError(ScriptErrorKind.SyntaxError, 2, "invalid character '$'"), result.Error);
    }
}
