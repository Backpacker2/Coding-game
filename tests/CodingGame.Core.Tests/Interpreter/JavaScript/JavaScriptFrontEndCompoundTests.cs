using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.JavaScript;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter.JavaScript;

public class JavaScriptFrontEndCompoundTests
{
    [Theory]
    [InlineData("if (ripe()) {\n    harvest();\n}", "1: if ripe()\n  2: harvest()")]
    [InlineData("if (ripe()) {\n harvest();\n} else {\n move(EAST);\n}", "1: if ripe()\n  2: harvest()\nelse\n  4: move(EAST)")]
    [InlineData(
        "if (a()) {\n x();\n} else if (b()) {\n y();\n} else {\n z();\n}",
        "1: if a()\n  2: x()\nelse\n  3: if b()\n    4: y()\n  else\n    6: z()")]
    [InlineData("while (true) {\n harvest();\n}", "1: while true\n  2: harvest()")]
    [InlineData("while (ripe()) {}", "1: while ripe()")]
    [InlineData("if (a()) {\n if (b()) {\n c();\n }\n d();\n}\ne();", "1: if a()\n  2: if b()\n    3: c()\n  5: d()\n7: e()")]
    [InlineData("if (a()) {\n // c\n\n b();\n}", "1: if a()\n  4: b()")]
    [InlineData("if (a()) { b(); }", "1: if a()\n  1: b()")]
    public void ParsesCompoundStatementsIntoTheExpectedTree(string source, string expectedDump)
    {
        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Null(result.Error);
        Assert.Equal(expectedDump, TreeDump.Of(result.Program!));
    }

    [Theory]
    [InlineData("if ripe() {\n harvest();\n}", ScriptErrorKind.SyntaxError, 1, "expected '('")]
    [InlineData("if (ripe() {\n}", ScriptErrorKind.SyntaxError, 1, "Unexpected token '{'")]
    [InlineData("if (ripe())\n harvest();", ScriptErrorKind.SyntaxError, 2, "expected '{', braces are required around the body")]
    [InlineData("if (ripe()) harvest();", ScriptErrorKind.SyntaxError, 1, "expected '{', braces are required around the body")]
    [InlineData("if (a()) {\n b();\n} else\n c();", ScriptErrorKind.SyntaxError, 4, "expected '{', braces are required around the body")]
    [InlineData("while (true)\n harvest();", ScriptErrorKind.SyntaxError, 2, "expected '{', braces are required around the body")]
    [InlineData("if (ripe()) {", ScriptErrorKind.SyntaxError, 1, "Unexpected end of input")]
    [InlineData("if (ripe()) {\n harvest();", ScriptErrorKind.SyntaxError, 2, "Unexpected end of input")]
    [InlineData("else {\n a();\n}", ScriptErrorKind.SyntaxError, 1, "Unexpected token 'else'")]
    [InlineData("if (a()) {\n b();\n} else {\n c();\n} else {\n d();\n}", ScriptErrorKind.SyntaxError, 5, "Unexpected token 'else'")]
    [InlineData("if () {\n}", ScriptErrorKind.SyntaxError, 1, "Unexpected token ')'")]
    [InlineData("while () {\n}", ScriptErrorKind.SyntaxError, 1, "Unexpected token ')'")]
    [InlineData("if (a()) {\n}\n}", ScriptErrorKind.SyntaxError, 3, "Unexpected token '}'")]
    [InlineData("if (a and b) {\n}", ScriptErrorKind.SyntaxError, 1, "Unexpected identifier 'and'")]
    public void ReportsTheFirstErrorWithItsLineAndDetail(string source, ScriptErrorKind kind, int line, string detail)
    {
        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Null(result.Program);
        Assert.Equal(new ScriptError(kind, line, detail), result.Error);
    }

    [Fact]
    public void RejectsBlocksNestedDeeperThanTheLimit()
    {
        string source = string.Concat(Enumerable.Repeat("if (true) {\n", 101)) + "harvest();";

        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Null(result.Program);
        Assert.Equal(new ScriptError(ScriptErrorKind.SyntaxError, 101, "too many nested levels"), result.Error);
    }

    [Fact]
    public void AcceptsBlocksNestedAtTheLimit()
    {
        string source = string.Concat(Enumerable.Repeat("if (true) {\n", 100)) + string.Concat(Enumerable.Repeat("}\n", 100));

        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Null(result.Error);
    }

    [Fact]
    public void RejectsLongElseIfChainsWithoutOverflowingTheStack()
    {
        string source = "if (a) {}" + string.Concat(Enumerable.Repeat(" else if (a) {}", 100_000));

        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Null(result.Program);
        Assert.Equal(new ScriptError(ScriptErrorKind.SyntaxError, 1, "too many nested levels"), result.Error);
    }

    [Fact]
    public void AcceptsElseIfChainsAtTheLimit()
    {
        string source = "if (a) {}" + string.Concat(Enumerable.Repeat(" else if (a) {}", 50));

        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Null(result.Error);
    }
}
