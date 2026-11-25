namespace CodingGame.Core.Interpreter.Python;

public sealed partial class PythonFrontEnd
{
    private If ParseIf()
    {
        Token keyword = Advance();
        Expression condition = ParseExpression();
        IReadOnlyList<Statement> then = ParseBlock($"'{keyword.Text}' statement", keyword.Line);

        IReadOnlyList<Statement> otherwise = [];
        Token next = Current;
        if (next is { Kind: TokenKind.Keyword, Text: "elif" })
        {
            otherwise = [ParseIf()];
        }
        else if (next is { Kind: TokenKind.Keyword, Text: "else" })
        {
            Advance();
            otherwise = ParseBlock("'else' statement", next.Line);
        }

        return new If(keyword.Line, condition, then, otherwise);
    }

    private While ParseWhile()
    {
        Token keyword = Advance();
        Expression condition = ParseExpression();
        IReadOnlyList<Statement> body = ParseBlock("'while' statement", keyword.Line);
        return new While(keyword.Line, condition, body);
    }

    private Repeat ParseFor()
    {
        Token keyword = Advance();
        ExpectName();
        Expect(TokenKind.Keyword, "in");

        Token range = Current;
        if (range is not { Kind: TokenKind.Name, Text: "range" })
        {
            throw Failure(ScriptErrorKind.NotUnlocked, range.Line, "for without range");
        }

        Advance();
        ExpectOperator("(");
        if (IsOperator(")"))
        {
            throw Failure(ScriptErrorKind.NotUnlocked, Current.Line, "range with other arguments");
        }

        Expression count = ParseExpression();
        if (IsOperator(","))
        {
            throw Failure(ScriptErrorKind.NotUnlocked, Current.Line, "range with other arguments");
        }

        Expect(TokenKind.Operator, ")");
        IReadOnlyList<Statement> body = ParseBlock("'for' statement", keyword.Line);
        return new Repeat(keyword.Line, count, body);
    }

    private FunctionDefinition ParseFunctionDefinition()
    {
        Token keyword = Advance();
        string name = ExpectName().Text;
        ExpectOperator("(");
        if (Current.Kind == TokenKind.Name)
        {
            throw Failure(ScriptErrorKind.NotUnlocked, Current.Line, "parameters");
        }

        Expect(TokenKind.Operator, ")");
        IReadOnlyList<Statement> body = ParseBlock("function definition", keyword.Line);
        return new FunctionDefinition(keyword.Line, name, body);
    }

    private List<Statement> ParseBlock(string description, int headerLine)
    {
        ExpectOperator(":");
        Token newline = Current;
        if (newline.Kind != TokenKind.Newline)
        {
            throw Failure(ScriptErrorKind.SyntaxError, newline.Line, "the body must start on a new line");
        }

        Advance();
        Token indent = Current;
        if (indent.Kind != TokenKind.Indent)
        {
            throw Failure(
                ScriptErrorKind.IndentationError,
                indent.Line,
                $"expected an indented block after {description} on line {headerLine}");
        }

        Advance();
        var body = new List<Statement>();
        while (Current.Kind != TokenKind.Dedent)
        {
            ParseStatement(body);
        }

        Advance();
        return body;
    }

    private Token ExpectName()
    {
        Token token = Current;
        if (token.Kind != TokenKind.Name)
        {
            throw UnexpectedToken(token);
        }

        Advance();
        return token;
    }

    private void ExpectOperator(string text)
    {
        Token token = Current;
        if (!IsOperator(text))
        {
            throw Failure(ScriptErrorKind.SyntaxError, token.Line, $"expected '{text}'");
        }

        Advance();
    }
}
