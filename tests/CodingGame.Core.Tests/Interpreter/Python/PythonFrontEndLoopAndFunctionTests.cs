using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.Python;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter.Python;

public class PythonFrontEndLoopAndFunctionTests
{
    [Theory]
    [InlineData("for i in range(3):\n    harvest()", "1: repeat 3\n  2: harvest()")]
    [InlineData(
        "for y in range(3):\n    for x in range(3):\n        farm()\n    move(NORTH)",
        "1: repeat 3\n  2: repeat 3\n    3: farm()\n  4: move(NORTH)")]
    [InlineData("for _ in range(2):\n    pass", "1: repeat 2")]
    [InlineData("for i in range(f()):\n    pass", "1: repeat f()")]
    [InlineData("def farm():\n    harvest()", "1: def farm\n  2: harvest()")]
    [InlineData("def f():\n    pass\nf()", "1: def f\n3: f()")]
    [InlineData("def f():\n    if ripe():\n        harvest()", "1: def f\n  2: if ripe()\n    3: harvest()")]
    public void ParsesLoopsAndFunctionsIntoTheExpectedTree(string source, string expectedDump)
    {
        ParseResult result = PythonFrontEnd.Parse(source);

        Assert.Null(result.Error);
        Assert.Equal(expectedDump, TreeDump.Of(result.Program!));
    }

    [Fact]
    public void ParsesTheFarmExampleFromTheLanguageDocument()
    {
        const string source = """
        def farm_tile():
            if ripe():
                harvest()
                if ground() == SOIL:
                    plant(POTATO)
                else:
                    plant(RADISH)

        while True:
            for y in range(3):
                for x in range(3):
                    farm_tile()
                    move(EAST)
                move(NORTH)
        """;
        var expectedDump = string.Join(
            "\n",
            "1: def farm_tile",
            "  2: if ripe()",
            "    3: harvest()",
            "    4: if (ground() == SOIL)",
            "      5: plant(POTATO)",
            "    else",
            "      7: plant(RADISH)",
            "9: while true",
            "  10: repeat 3",
            "    11: repeat 3",
            "      12: farm_tile()",
            "      13: move(EAST)",
            "    14: move(NORTH)");

        ParseResult result = PythonFrontEnd.Parse(source);

        Assert.Null(result.Error);
        Assert.Equal(expectedDump, TreeDump.Of(result.Program!));
    }

    [Fact]
    public void RepeatAndCountUseTheLineOfTheLoop()
    {
        ParseResult result = PythonFrontEnd.Parse("\nfor i in range(2):\n    pass");

        var repeat = Assert.IsType<Repeat>(Assert.Single(result.Program!.Body));
        Assert.Equal(2, repeat.Line);
        Assert.Equal(2, repeat.Count.Line);
    }

    [Theory]
    [InlineData("for i in items:\n    pass", ScriptErrorKind.NotInGame, 1, "for without range")]
    [InlineData("for i in range(1, 3):\n    pass", ScriptErrorKind.NotInGame, 1, "range with other arguments")]
    [InlineData("for i in range():\n    pass", ScriptErrorKind.NotInGame, 1, "range with other arguments")]
    [InlineData("for i in range:\n    pass", ScriptErrorKind.SyntaxError, 1, "expected '('")]
    [InlineData("for in range(3):\n  pass", ScriptErrorKind.SyntaxError, 1, "invalid syntax")]
    [InlineData("for i range(3):\n  pass", ScriptErrorKind.SyntaxError, 1, "invalid syntax")]
    [InlineData("for i in range(3:\n  pass", ScriptErrorKind.SyntaxError, 1, "invalid syntax")]
    [InlineData("for i in range(3)\n    pass", ScriptErrorKind.SyntaxError, 1, "expected ':'")]
    [InlineData("def f(x):\n    pass", ScriptErrorKind.NotInGame, 1, "parameters")]
    [InlineData("def f:\n    pass", ScriptErrorKind.SyntaxError, 1, "expected '('")]
    [InlineData("def f()\n    pass", ScriptErrorKind.SyntaxError, 1, "expected ':'")]
    [InlineData("def ():\n    pass", ScriptErrorKind.SyntaxError, 1, "invalid syntax")]
    [InlineData("def if():\n    pass", ScriptErrorKind.SyntaxError, 1, "invalid syntax")]
    [InlineData("def f():\nharvest()", ScriptErrorKind.IndentationError, 2, "expected an indented block after function definition on line 1")]
    [InlineData("for i in range(3):\nharvest()", ScriptErrorKind.IndentationError, 2, "expected an indented block after 'for' statement on line 1")]
    public void ReportsTheFirstErrorOfALoopOrFunction(string source, ScriptErrorKind kind, int line, string detail)
    {
        ParseResult result = PythonFrontEnd.Parse(source);

        Assert.Null(result.Program);
        Assert.Equal(new ScriptError(kind, line, detail), result.Error);
    }
}
