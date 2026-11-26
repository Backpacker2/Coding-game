using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.JavaScript;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter.JavaScript;

public class JavaScriptFrontEndStatementTests
{
    [Fact]
    public void KeywordsContainReservedWordsOnly()
    {
        Assert.Contains("while", JavaScriptFrontEnd.Keywords);
        Assert.Contains("function", JavaScriptFrontEnd.Keywords);
        Assert.Contains("true", JavaScriptFrontEnd.Keywords);
        Assert.DoesNotContain("def", JavaScriptFrontEnd.Keywords);
    }

    [Theory]
    [InlineData("harvest();", "1: harvest()")]
    [InlineData("move(EAST);", "1: move(EAST)")]
    [InlineData("// start\nharvest();\n\nmove(NORTH);  // go\n", "2: harvest()\n4: move(NORTH)")]
    [InlineData("plant(RADISH);\nf(1, true, false);", "1: plant(RADISH)\n2: f(1, true, false)")]
    [InlineData("ground() === SOIL;", "1: (ground() == SOIL)")]
    [InlineData("ripe() !== false;", "1: (ripe() != false)")]
    [InlineData("f(g(1), 2);", "1: f(g(1), 2)")]
    [InlineData("NORTH;", "1: NORTH")]
    [InlineData("harvest()\n;", "1: harvest()")]
    [InlineData("move(\n  EAST\n);", "1: move(EAST)")]
    [InlineData("print(x);", "1: print(x)")]
    [InlineData("move(True);", "1: move(True)")]
    public void ParsesStatementsIntoTheExpectedTree(string source, string expectedDump)
    {
        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Null(result.Error);
        Assert.Equal(expectedDump, TreeDump.Of(result.Program!));
    }

    [Fact]
    public void EmptySourceGivesAnEmptyJavaScriptProgram()
    {
        ParseResult result = JavaScriptFrontEnd.Parse(string.Empty);

        Assert.Null(result.Error);
        Assert.Equal(ScriptLanguage.JavaScript, result.Program!.Language);
        Assert.Empty(result.Program.Body);
    }

    [Fact]
    public void StatementLineIsTheLineOfItsFirstToken()
    {
        ParseResult result = JavaScriptFrontEnd.Parse("\n\nmove(EAST);");

        var statement = Assert.IsType<ExpressionStatement>(Assert.Single(result.Program!.Body));
        var call = Assert.IsType<Call>(statement.Expression);
        Assert.Equal(3, statement.Line);
        Assert.Equal(3, call.Line);
        Assert.Equal(3, call.Arguments[0].Line);
    }

    [Fact]
    public void CompareLineIsTheLineOfItsLeftOperand()
    {
        ParseResult result = JavaScriptFrontEnd.Parse("\nground() === SOIL;");

        var statement = Assert.IsType<ExpressionStatement>(Assert.Single(result.Program!.Body));
        var compare = Assert.IsType<Compare>(statement.Expression);
        Assert.Equal(2, compare.Line);
    }

    [Theory]
    [InlineData("harvest()", ScriptErrorKind.SyntaxError, 1, "expected ';'")]
    [InlineData("harvest()\nmove(EAST);", ScriptErrorKind.SyntaxError, 1, "expected ';'")]
    [InlineData("harvest() move(EAST);", ScriptErrorKind.SyntaxError, 1, "Unexpected identifier 'move'")]
    [InlineData("move(EAST EAST);", ScriptErrorKind.SyntaxError, 1, "Unexpected identifier 'EAST'")]
    [InlineData("harvest());", ScriptErrorKind.SyntaxError, 1, "Unexpected token ')'")]
    [InlineData(");", ScriptErrorKind.SyntaxError, 1, "Unexpected token ')'")]
    [InlineData("harvest(", ScriptErrorKind.SyntaxError, 1, "Unexpected end of input")]
    [InlineData("move(,);", ScriptErrorKind.SyntaxError, 1, "Unexpected token ','")]
    [InlineData("f(1,);", ScriptErrorKind.SyntaxError, 1, "Unexpected token ')'")]
    [InlineData("===;", ScriptErrorKind.SyntaxError, 1, "Unexpected token '==='")]
    [InlineData("ripe() ===;", ScriptErrorKind.SyntaxError, 1, "Unexpected token ';'")]
    [InlineData("move(1 2);", ScriptErrorKind.SyntaxError, 1, "Unexpected number")]
    [InlineData("(a);", ScriptErrorKind.SyntaxError, 1, "Unexpected token '('")]
    [InlineData(";", ScriptErrorKind.SyntaxError, 1, "Unexpected token ';'")]
    [InlineData("}", ScriptErrorKind.SyntaxError, 1, "Unexpected token '}'")]
    [InlineData("{", ScriptErrorKind.SyntaxError, 1, "Unexpected token '{'")]
    [InlineData("harvest();\nmove(EAST EAST);", ScriptErrorKind.SyntaxError, 2, "Unexpected identifier 'EAST'")]
    [InlineData("a === b === c;", ScriptErrorKind.NotInGame, 1, "chained comparisons")]
    [InlineData("a !== b === c;", ScriptErrorKind.NotInGame, 1, "chained comparisons")]
    public void ReportsTheFirstErrorWithItsLineAndDetail(string source, ScriptErrorKind kind, int line, string detail)
    {
        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Null(result.Program);
        Assert.Equal(new ScriptError(kind, line, detail), result.Error);
    }

    [Fact]
    public void RejectsNestingDeeperThanTheLimitWithoutOverflowingTheStack()
    {
        string source = string.Concat(Enumerable.Repeat("f(", 100_000)) + string.Concat(Enumerable.Repeat(")", 100_000)) + ";";

        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Null(result.Program);
        Assert.Equal(new ScriptError(ScriptErrorKind.SyntaxError, 1, "too many nested levels"), result.Error);
    }

    [Fact]
    public void AcceptsNestingAtTheLimit()
    {
        string source = string.Concat(Enumerable.Repeat("f(", 100)) + string.Concat(Enumerable.Repeat(")", 100)) + ";";

        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Null(result.Error);
    }
}
