using CodingGame.Core.Interpreter.Python;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter.Python;

public class PythonTokenizerTests
{
    [Theory]
    [InlineData("", "EOF@1")]
    [InlineData("harvest()", "Name:harvest@1 Op:(@1 Op:)@1 NL@1 EOF@1")]
    [InlineData("move(EAST)\n", "Name:move@1 Op:(@1 Name:EAST@1 Op:)@1 NL@1 EOF@1")]
    [InlineData("# hi\n\nharvest() # done\n   \n", "Name:harvest@3 Op:(@3 Op:)@3 NL@3 EOF@3")]
    [InlineData("a()\r\nb()\r\n", "Name:a@1 Op:(@1 Op:)@1 NL@1 Name:b@2 Op:(@2 Op:)@2 NL@2 EOF@2")]
    [InlineData(
        "if ripe():\n    harvest()\nmove(EAST)",
        "Keyword:if@1 Name:ripe@1 Op:(@1 Op:)@1 Op::@1 NL@1 INDENT@2 Name:harvest@2 Op:(@2 Op:)@2 NL@2 DEDENT@3 Name:move@3 Op:(@3 Name:EAST@3 Op:)@3 NL@3 EOF@3")]
    [InlineData(
        "while True:\n  if x:\n    a()\nb()",
        "Keyword:while@1 Keyword:True@1 Op::@1 NL@1 INDENT@2 Keyword:if@2 Name:x@2 Op::@2 NL@2 INDENT@3 Name:a@3 Op:(@3 Op:)@3 NL@3 DEDENT@4 DEDENT@4 Name:b@4 Op:(@4 Op:)@4 NL@4 EOF@4")]
    [InlineData(
        "if x:\n    a()",
        "Keyword:if@1 Name:x@1 Op::@1 NL@1 INDENT@2 Name:a@2 Op:(@2 Op:)@2 NL@2 DEDENT@2 EOF@2")]
    [InlineData(
        "if x:\n    a()\n# c\n\n    b()",
        "Keyword:if@1 Name:x@1 Op::@1 NL@1 INDENT@2 Name:a@2 Op:(@2 Op:)@2 NL@2 Name:b@5 Op:(@5 Op:)@5 NL@5 DEDENT@5 EOF@5")]
    [InlineData("a == b != c", "Name:a@1 Op:==@1 Name:b@1 Op:!=@1 Name:c@1 NL@1 EOF@1")]
    [InlineData("x = 1", "Name:x@1 Op:=@1 Number:1@1 NL@1 EOF@1")]
    [InlineData("1 // 2 ** 3 <= 4", "Number:1@1 Op://@1 Number:2@1 Op:**@1 Number:3@1 Op:<=@1 Number:4@1 NL@1 EOF@1")]
    [InlineData("pass import print True true", "Keyword:pass@1 Keyword:import@1 Name:print@1 Keyword:True@1 Name:true@1 NL@1 EOF@1")]
    [InlineData("farm_tile2()", "Name:farm_tile2@1 Op:(@1 Op:)@1 NL@1 EOF@1")]
    public void TokenizesSourceIntoTokensWithoutError(string source, string expected)
    {
        var result = PythonTokenizer.Tokenize(source);

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
            TokenKind.Newline => $"NL@{token.Line}",
            TokenKind.Indent => $"INDENT@{token.Line}",
            TokenKind.Dedent => $"DEDENT@{token.Line}",
            TokenKind.EndOfFile => $"EOF@{token.Line}",
            _ => throw new InvalidOperationException($"unexpected token kind {token.Kind}"),
        };
    }
}
