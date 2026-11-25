using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.Python;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter.Python;

public class PythonTokenizerErrorTests
{
    [Theory]
    [InlineData("if x:\n\tharvest()", ScriptErrorKind.IndentationError, 2, "tabs are not allowed in indentation, use spaces")]
    [InlineData("if x:\n \tharvest()", ScriptErrorKind.IndentationError, 2, "tabs are not allowed in indentation, use spaces")]
    [InlineData("if x:\n        a()\n    b()", ScriptErrorKind.IndentationError, 3, "unindent does not match any outer indentation level")]
    [InlineData("x = 'hi'", ScriptErrorKind.NotUnlocked, 1, "strings")]
    [InlineData("x = \"hi\"", ScriptErrorKind.NotUnlocked, 1, "strings")]
    [InlineData("a = 1.5", ScriptErrorKind.NotUnlocked, 1, "decimal numbers")]
    [InlineData("a = 1.", ScriptErrorKind.NotUnlocked, 1, "decimal numbers")]
    [InlineData("12abc", ScriptErrorKind.SyntaxError, 1, "invalid decimal literal")]
    [InlineData("0x1F", ScriptErrorKind.SyntaxError, 1, "invalid decimal literal")]
    [InlineData("007", ScriptErrorKind.SyntaxError, 1, "leading zeros in decimal integer literals are not permitted")]
    [InlineData("00", ScriptErrorKind.SyntaxError, 1, "leading zeros in decimal integer literals are not permitted")]
    [InlineData("99999999999", ScriptErrorKind.SyntaxError, 1, "integer is too large")]
    [InlineData("move($)", ScriptErrorKind.SyntaxError, 1, "invalid character '$'")]
    [InlineData("a ! b", ScriptErrorKind.SyntaxError, 1, "invalid character '!'")]
    [InlineData("a \\ b", ScriptErrorKind.SyntaxError, 1, "invalid character '\\'")]
    [InlineData("é", ScriptErrorKind.SyntaxError, 1, "invalid character 'é'")]
    [InlineData("`x`", ScriptErrorKind.SyntaxError, 1, "invalid character '`'")]
    public void ReportsTheFirstProblemInTheSource(string source, ScriptErrorKind kind, int line, string detail)
    {
        var result = PythonTokenizer.Tokenize(source);

        Assert.Equal(new ScriptError(kind, line, detail), result.Error);
    }

    [Theory]
    [InlineData("0", "Number:0@1 NL@1 EOF@1")]
    [InlineData("2147483647", "Number:2147483647@1 NL@1 EOF@1")]
    public void AcceptsValidNumbers(string source, string expected)
    {
        var result = PythonTokenizer.Tokenize(source);

        Assert.Null(result.Error);
        Assert.Equal(expected, Render(result.Tokens));
    }

    [Fact]
    public void IgnoresTabsInALineWithOnlyWhitespace()
    {
        var result = PythonTokenizer.Tokenize("if x:\n\t\n    a()");

        Assert.Null(result.Error);
    }

    [Fact]
    public void KeepsTheTokensBeforeTheProblemAndEndsOnItsLine()
    {
        var result = PythonTokenizer.Tokenize("harvest()\nmove($)");

        Assert.Equal("Name:harvest@1 Op:(@1 Op:)@1 NL@1 Name:move@2 Op:(@2 EOF@2", Render(result.Tokens));
        Assert.Equal(new ScriptError(ScriptErrorKind.SyntaxError, 2, "invalid character '$'"), result.Error);
    }

    private static string Render(IReadOnlyList<Token> tokens)
    {
        return string.Join(" ", tokens.Select(RenderToken));
    }

    private static string RenderToken(Token token)
    {
        return token.Kind switch
        {
            TokenKind.Name => $"Name:{token.Text}@{token.Line}",
            TokenKind.Keyword => $"Keyword:{token.Text}@{token.Line}",
            TokenKind.Number => $"Number:{token.Text}@{token.Line}",
            TokenKind.Operator => $"Op:{token.Text}@{token.Line}",
            TokenKind.Newline => $"NL@{token.Line}",
            TokenKind.Indent => $"INDENT@{token.Line}",
            TokenKind.Dedent => $"DEDENT@{token.Line}",
            TokenKind.EndOfFile => $"EOF@{token.Line}",
            _ => throw new InvalidOperationException($"unexpected token kind {token.Kind}"),
        };
    }
}
