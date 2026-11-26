using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.JavaScript;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter.JavaScript;

public class JavaScriptFrontEndNotInGameTests
{
    [Theory]
    [InlineData("import x;", 1, "import")]
    [InlineData("return;", 1, "return")]
    [InlineData("var x = 1;", 1, "var")]
    [InlineData("const x = 1;", 1, "const")]
    [InlineData("x += 1;", 1, "+=")]
    [InlineData("x++;", 1, "++")]
    [InlineData("if (a && b) {\n}", 1, "&&")]
    [InlineData("if (a || b) {\n}", 1, "||")]
    [InlineData("if (!ripe()) {\n}", 1, "!")]
    [InlineData("while (null) {\n}", 1, "null")]
    [InlineData("lst[0];", 1, "[")]
    [InlineData("console.log(x);", 1, ".")]
    [InlineData("class A {}", 1, "class")]
    [InlineData("try {}", 1, "try")]
    [InlineData("break;", 1, "break")]
    [InlineData("continue;", 1, "continue")]
    [InlineData("x => x;", 1, "=>")]
    [InlineData("a ? b : c;", 1, "?")]
    [InlineData("typeof x;", 1, "typeof")]
    [InlineData("new A();", 1, "new")]
    [InlineData("this;", 1, "this")]
    [InlineData("harvest();\nimport x;", 2, "import")]
    public void ReportsSyntaxThatIsNotInTheGameYet(string source, int line, string feature)
    {
        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Equal(new ScriptError(ScriptErrorKind.NotInGame, line, feature), result.Error);
    }

    [Theory]
    [InlineData("a == b;", 1, "==", "===")]
    [InlineData("a != b;", 1, "!=", "!==")]
    [InlineData("if (ripe() == true) {\n}", 1, "==", "===")]
    [InlineData("while (a != b) {\n}", 1, "!=", "!==")]
    [InlineData("harvest();\nx == y;", 2, "==", "===")]
    public void ReportsLooseEqualityAsASyntaxErrorThatNamesTheStrictOperator(string source, int line, string loose, string strict)
    {
        string detail = $"'{loose}' is not allowed, use '{strict}': '{loose}' converts types before it compares";

        Assert.Equal(new ScriptError(ScriptErrorKind.SyntaxError, line, detail), JavaScriptFrontEnd.Parse(source).Error);
    }

    [Theory]
    [InlineData("import x;", "Error on line 1: 'import' is not part of this game yet")]
    [InlineData("a == b;", "SyntaxError on line 1: '==' is not allowed, use '===': '==' converts types before it compares")]
    [InlineData(");", "SyntaxError on line 1: Unexpected token ')'")]
    [InlineData("harvest()", "SyntaxError on line 1: expected ';'")]
    public void ShowsTheMessageInJavaScriptConventions(string source, string expectedMessage)
    {
        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.NotNull(result.Error);
        Assert.Equal(expectedMessage, result.Error.ToMessage(ScriptLanguage.JavaScript));
    }

    [Theory]
    [InlineData("move(EAST) harvest();\nmove(#);", ScriptErrorKind.SyntaxError, 1, "Unexpected identifier 'harvest'")]
    [InlineData("harvest();\nmove(#);", ScriptErrorKind.SyntaxError, 2, "invalid character '#'")]
    [InlineData("harvest();\nlet x = `a`;", ScriptErrorKind.NotInGame, 2, "strings")]
    public void ReportsTheFirstErrorInSourceOrder(string source, ScriptErrorKind kind, int line, string detail)
    {
        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Equal(new ScriptError(kind, line, detail), result.Error);
    }
}
