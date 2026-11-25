using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.Python;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter.Python;

public class PythonFrontEndCompoundTests
{
    [Theory]
    [InlineData("if ripe():\n    harvest()", "1: if ripe()\n  2: harvest()")]
    [InlineData("if ripe():\n    harvest()\nelse:\n    move(EAST)", "1: if ripe()\n  2: harvest()\nelse\n  4: move(EAST)")]
    [InlineData(
        "if a():\n    x()\nelif b():\n    y()\nelse:\n    z()",
        "1: if a()\n  2: x()\nelse\n  3: if b()\n    4: y()\n  else\n    6: z()")]
    [InlineData("while True:\n    harvest()", "1: while true\n  2: harvest()")]
    [InlineData("while ripe():\n    pass", "1: while ripe()")]
    [InlineData("if a():\n    if b():\n        c()\n    d()\ne()", "1: if a()\n  2: if b()\n    3: c()\n  4: d()\n5: e()")]
    [InlineData("if a():\n    # c\n\n    b()", "1: if a()\n  4: b()")]
    public void ParsesBlocksIntoTheExpectedTree(string source, string expectedDump)
    {
        ParseResult result = PythonFrontEnd.Parse(source);

        Assert.Null(result.Error);
        Assert.Equal(expectedDump, TreeDump.Of(result.Program!));
    }

    [Theory]
    [InlineData("if ripe()\n    harvest()", ScriptErrorKind.SyntaxError, 1, "expected ':'")]
    [InlineData("if ripe():\nharvest()", ScriptErrorKind.IndentationError, 2, "expected an indented block after 'if' statement on line 1")]
    [InlineData("if ripe():", ScriptErrorKind.IndentationError, 1, "expected an indented block after 'if' statement on line 1")]
    [InlineData("if a():\n    b()\nelse:\nc()", ScriptErrorKind.IndentationError, 4, "expected an indented block after 'else' statement on line 3")]
    [InlineData("if a():\n    b()\nelif c():\nd()", ScriptErrorKind.IndentationError, 4, "expected an indented block after 'elif' statement on line 3")]
    [InlineData("while True:\nharvest()", ScriptErrorKind.IndentationError, 2, "expected an indented block after 'while' statement on line 1")]
    [InlineData("harvest()\n    move(EAST)", ScriptErrorKind.IndentationError, 2, "unexpected indent")]
    [InlineData("if a():\n    b()\n        c()", ScriptErrorKind.IndentationError, 3, "unexpected indent")]
    [InlineData("if ripe(): harvest()", ScriptErrorKind.SyntaxError, 1, "the body must start on a new line")]
    [InlineData("else:\n    a()", ScriptErrorKind.SyntaxError, 1, "invalid syntax")]
    [InlineData("if a():\n    b()\nelse:\n    c()\nelse:\n    d()", ScriptErrorKind.SyntaxError, 5, "invalid syntax")]
    [InlineData("if:\n    a()", ScriptErrorKind.SyntaxError, 1, "invalid syntax")]
    public void ReportsTheFirstErrorOfABlock(string source, ScriptErrorKind kind, int line, string detail)
    {
        ParseResult result = PythonFrontEnd.Parse(source);

        Assert.Null(result.Program);
        Assert.Equal(new ScriptError(kind, line, detail), result.Error);
    }
}
