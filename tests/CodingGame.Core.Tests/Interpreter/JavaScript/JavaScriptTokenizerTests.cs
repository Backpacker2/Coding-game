using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.JavaScript;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter.JavaScript;

public class JavaScriptTokenizerTests
{
    [Theory]
    [InlineData("", "EOF@1")]
    [InlineData("harvest();", "Name:harvest@1 Op:(@1 Op:)@1 Op:;@1 EOF@1")]
    [InlineData("// hi\n\nharvest(); // done\n   \n", "Name:harvest@3 Op:(@3 Op:)@3 Op:;@3 EOF@3")]
    [InlineData("a();\r\nb();\r\n", "Name:a@1 Op:(@1 Op:)@1 Op:;@1 Name:b@2 Op:(@2 Op:)@2 Op:;@2 EOF@2")]
    [InlineData("a();\rb();", "Name:a@1 Op:(@1 Op:)@1 Op:;@1 Name:b@2 Op:(@2 Op:)@2 Op:;@2 EOF@2")]
    [InlineData(
        "if (ripe()) {\n  harvest();\n}",
        "Keyword:if@1 Op:(@1 Name:ripe@1 Op:(@1 Op:)@1 Op:)@1 Op:{@1 Name:harvest@2 Op:(@2 Op:)@2 Op:;@2 Op:}@3 EOF@3")]
    [InlineData(
        "for (let i = 0; i < 3; i++) {}",
        "Keyword:for@1 Op:(@1 Keyword:let@1 Name:i@1 Op:=@1 Number:0@1 Op:;@1 Name:i@1 Op:<@1 Number:3@1 Op:;@1 Name:i@1 Op:++@1 Op:)@1 Op:{@1 Op:}@1 EOF@1")]
    [InlineData("a === b !== c", "Name:a@1 Op:===@1 Name:b@1 Op:!==@1 Name:c@1 EOF@1")]
    [InlineData("a == b != c", "Name:a@1 Op:==@1 Name:b@1 Op:!=@1 Name:c@1 EOF@1")]
    [InlineData("a ==== b", "Name:a@1 Op:===@1 Op:=@1 Name:b@1 EOF@1")]
    [InlineData("x <= y >= z && w || !v", "Name:x@1 Op:<=@1 Name:y@1 Op:>=@1 Name:z@1 Op:&&@1 Name:w@1 Op:||@1 Op:!@1 Name:v@1 EOF@1")]
    [InlineData("function var true True $a _b c2", "Keyword:function@1 Keyword:var@1 Keyword:true@1 Name:True@1 Name:$a@1 Name:_b@1 Name:c2@1 EOF@1")]
    [InlineData("a+++b", "Name:a@1 Op:++@1 Op:+@1 Name:b@1 EOF@1")]
    [InlineData("1 / 2", "Number:1@1 Op:/@1 Number:2@1 EOF@1")]
    [InlineData("a // c\nb", "Name:a@1 Name:b@2 EOF@2")]
    [InlineData("a;\n\n// end", "Name:a@1 Op:;@1 EOF@1")]
    [InlineData("\tharvest();", "Name:harvest@1 Op:(@1 Op:)@1 Op:;@1 EOF@1")]
    public void TokenizesSourceIntoTokensWithoutError(string source, string expected)
    {
        var result = JavaScriptTokenizer.Tokenize(source);

        Assert.Null(result.Error);
        Assert.Equal(expected, Render(result.Tokens));
    }

    [Fact]
    public void StopsAtTheFirstInvalidCharacterAndReportsItsLine()
    {
        var result = JavaScriptTokenizer.Tokenize("harvest();\nmove(#);");

        Assert.Equal("Name:harvest@1 Op:(@1 Op:)@1 Op:;@1 Name:move@2 Op:(@2 EOF@2", Render(result.Tokens));
        Assert.Equal(new ScriptError(ScriptErrorKind.SyntaxError, 2, "invalid character '#'"), result.Error);
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
