using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.JavaScript;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter.JavaScript;

public class JavaScriptFrontEndLoopAndFunctionTests
{
    [Theory]
    [InlineData("for (let i = 0; i < 3; i++) {\n harvest();\n}", "1: repeat 3\n  2: harvest()")]
    [InlineData(
        "for (let y = 0; y < 3; y++) {\n for (let x = 0; x < 3; x++) {\n farm();\n }\n move(NORTH);\n}",
        "1: repeat 3\n  2: repeat 3\n    3: farm()\n  5: move(NORTH)")]
    [InlineData("for (let i = 0; i < 2; i++) {}", "1: repeat 2")]
    [InlineData("for (let i = 0; i < f(); i++) {}", "1: repeat f()")]
    [InlineData("for (let i = 0; i < n; i++) {}", "1: repeat n")]
    [InlineData("for (let i = 0;\n i < 3;\n i++) {\n harvest();\n}", "1: repeat 3\n  4: harvest()")]
    [InlineData("function farm() {\n harvest();\n}", "1: def farm\n  2: harvest()")]
    [InlineData("function f() {}\nf();", "1: def f\n2: f()")]
    [InlineData("function f() {\n if (ripe()) {\n harvest();\n }\n}", "1: def f\n  2: if ripe()\n    3: harvest()")]
    public void ParsesLoopsAndFunctionsIntoTheExpectedTree(string source, string expectedDump)
    {
        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Null(result.Error);
        Assert.Equal(expectedDump, TreeDump.Of(result.Program!));
    }

    [Theory]
    [InlineData("farm();\nfunction farm() {\n move(EAST);\n}", "2: def farm\n  3: move(EAST)\n1: farm()")]
    [InlineData("a();\nfunction f() {}\nb();\nfunction g() {}\nc();", "2: def f\n4: def g\n1: a()\n3: b()\n5: c()")]
    [InlineData("function f() {\n g();\n function g() {}\n}", "1: def f\n  3: def g\n  2: g()")]
    [InlineData("if (a()) {\n g();\n function g() {}\n}", "1: if a()\n  3: def g\n  2: g()")]
    public void MovesFunctionDefinitionsToTheTopOfTheirBlock(string source, string expectedDump)
    {
        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Null(result.Error);
        Assert.Equal(expectedDump, TreeDump.Of(result.Program!));
    }

    [Fact]
    public void ParsesTheFarmExampleFromTheLanguageDocument()
    {
        const string source = """
            function farmTile() {
                if (ripe()) {
                    harvest();
                    if (ground() === SOIL) {
                        plant(POTATO);
                    } else {
                        plant(RADISH);
                    }
                }
            }

            while (true) {
                for (let y = 0; y < 3; y++) {
                    for (let x = 0; x < 3; x++) {
                        farmTile();
                        move(EAST);
                    }
                    move(NORTH);
                }
            }
            """;
        const string expectedDump = """
            1: def farmTile
              2: if ripe()
                3: harvest()
                4: if (ground() == SOIL)
                  5: plant(POTATO)
                else
                  7: plant(RADISH)
            12: while true
              13: repeat 3
                14: repeat 3
                  15: farmTile()
                  16: move(EAST)
                18: move(NORTH)
            """;

        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Null(result.Error);
        Assert.Equal(expectedDump, TreeDump.Of(result.Program!));
    }

    [Fact]
    public void RepeatLineAndCountLineAreTheLineOfTheForKeyword()
    {
        ParseResult result = JavaScriptFrontEnd.Parse("\nfor (let i = 0; i < 2; i++) {}");

        var repeat = Assert.IsType<Repeat>(Assert.Single(result.Program!.Body));
        Assert.Equal(2, repeat.Line);
        Assert.Equal(2, repeat.Count.Line);
    }

    [Theory]
    [InlineData("for (let i = 0; i <= 3; i++) {}", ScriptErrorKind.NotUnlocked, 1, "other for loops")]
    [InlineData("for (let i = 1; i < 3; i++) {}", ScriptErrorKind.NotUnlocked, 1, "other for loops")]
    [InlineData("for (let i = 0; i < 3; i += 1) {}", ScriptErrorKind.NotUnlocked, 1, "other for loops")]
    [InlineData("for (let i = 0; i < 3; i--) {}", ScriptErrorKind.NotUnlocked, 1, "other for loops")]
    [InlineData("for (let i = 0; j < 3; i++) {}", ScriptErrorKind.NotUnlocked, 1, "other for loops")]
    [InlineData("for (let i = 0; i < 3; j++) {}", ScriptErrorKind.NotUnlocked, 1, "other for loops")]
    [InlineData("for (var i = 0; i < 3; i++) {}", ScriptErrorKind.NotUnlocked, 1, "other for loops")]
    [InlineData("for (i = 0; i < 3; i++) {}", ScriptErrorKind.NotUnlocked, 1, "other for loops")]
    [InlineData("for (;;) {}", ScriptErrorKind.NotUnlocked, 1, "other for loops")]
    [InlineData("for (let i of items) {}", ScriptErrorKind.SyntaxError, 1, "expected '='")]
    [InlineData("for let i = 0; i < 3; i++ {}", ScriptErrorKind.SyntaxError, 1, "expected '('")]
    [InlineData("for (let i = 0 i < 3; i++) {}", ScriptErrorKind.SyntaxError, 1, "expected ';'")]
    [InlineData("for (let i = 0; i < 3 i++) {}", ScriptErrorKind.SyntaxError, 1, "expected ';'")]
    [InlineData("for (let i = 0; i < ; i++) {}", ScriptErrorKind.SyntaxError, 1, "Unexpected token ';'")]
    [InlineData("for (let i = 0; i < 3; i++ {}", ScriptErrorKind.SyntaxError, 1, "Unexpected token '{'")]
    [InlineData("for (let i = 0; i < 3; i++)\n harvest();", ScriptErrorKind.SyntaxError, 2, "expected '{', braces are required around the body")]
    [InlineData("function f(x) {}", ScriptErrorKind.NotUnlocked, 1, "parameters")]
    [InlineData("function f {}", ScriptErrorKind.SyntaxError, 1, "expected '('")]
    [InlineData("function () {}", ScriptErrorKind.SyntaxError, 1, "Unexpected token '('")]
    [InlineData("function if() {}", ScriptErrorKind.SyntaxError, 1, "Unexpected token 'if'")]
    [InlineData("function f()\n harvest();", ScriptErrorKind.SyntaxError, 2, "expected '{', braces are required around the body")]
    [InlineData("function f() {", ScriptErrorKind.SyntaxError, 1, "Unexpected end of input")]
    public void ReportsTheFirstErrorWithItsLineAndDetail(string source, ScriptErrorKind kind, int line, string detail)
    {
        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Null(result.Program);
        Assert.Equal(new ScriptError(kind, line, detail), result.Error);
    }
}
