using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.JavaScript;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter.JavaScript;

public class JavaScriptTokenizerErrorTests
{
    [Theory]
    [InlineData("x = 'hi';", ScriptErrorKind.NotInGame, 1, "strings")]
    [InlineData("x = \"hi\";", ScriptErrorKind.NotInGame, 1, "strings")]
    [InlineData("x = `hi`;", ScriptErrorKind.NotInGame, 1, "strings")]
    [InlineData("a = 1.5;", ScriptErrorKind.NotInGame, 1, "decimal numbers")]
    [InlineData("a = 1.;", ScriptErrorKind.NotInGame, 1, "decimal numbers")]
    [InlineData("/* c */ harvest();", ScriptErrorKind.NotInGame, 1, "block comments")]
    [InlineData("harvest();\n/*", ScriptErrorKind.NotInGame, 2, "block comments")]
    [InlineData("12abc", ScriptErrorKind.SyntaxError, 1, "Invalid or unexpected token")]
    [InlineData("0x1F", ScriptErrorKind.SyntaxError, 1, "Invalid or unexpected token")]
    [InlineData("1_000", ScriptErrorKind.SyntaxError, 1, "Invalid or unexpected token")]
    [InlineData("5$", ScriptErrorKind.SyntaxError, 1, "Invalid or unexpected token")]
    [InlineData("007", ScriptErrorKind.SyntaxError, 1, "leading zeros are not allowed in numbers")]
    [InlineData("00", ScriptErrorKind.SyntaxError, 1, "leading zeros are not allowed in numbers")]
    [InlineData("99999999999", ScriptErrorKind.SyntaxError, 1, "number is too large")]
    [InlineData("move(#);", ScriptErrorKind.SyntaxError, 1, "invalid character '#'")]
    [InlineData("a @ b", ScriptErrorKind.SyntaxError, 1, "invalid character '@'")]
    [InlineData("a \\ b", ScriptErrorKind.SyntaxError, 1, "invalid character '\\'")]
    [InlineData("é", ScriptErrorKind.SyntaxError, 1, "invalid character 'é'")]
    [InlineData("x;\n\n  é", ScriptErrorKind.SyntaxError, 3, "invalid character 'é'")]
    public void ReportsTheFirstProblemWithItsKindLineAndDetail(
        string source,
        ScriptErrorKind kind,
        int line,
        string detail)
    {
        var result = JavaScriptTokenizer.Tokenize(source);

        Assert.Equal(new ScriptError(kind, line, detail), result.Error);
        Assert.Equal(TokenKind.EndOfFile, result.Tokens[^1].Kind);
        Assert.Equal(line, result.Tokens[^1].Line);
    }

    [Theory]
    [InlineData("0", "Number:0@1 EOF@1")]
    [InlineData("2147483647", "Number:2147483647@1 EOF@1")]
    [InlineData("move($);", "Name:move@1 Op:(@1 Name:$@1 Op:)@1 Op:;@1 EOF@1")]
    [InlineData("// \"quoted\" 'text' /* not a block */", "EOF@1")]
    public void AcceptsValidNumbersAndCommentsWithoutError(string source, string expected)
    {
        var result = JavaScriptTokenizer.Tokenize(source);

        Assert.Null(result.Error);
        Assert.Equal(expected, Render(result.Tokens));
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
            TokenKind.EndOfFile => $"EOF@{token.Line}",
            _ => throw new InvalidOperationException($"unexpected token kind {token.Kind}"),
        };
    }
}
