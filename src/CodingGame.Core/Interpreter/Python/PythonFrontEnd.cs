namespace CodingGame.Core.Interpreter.Python;

/// <summary>
/// Parses Python source into a <see cref="ScriptProgram"/>. Only the part of Python the game teaches is accepted;
/// anything else gives an error with a line number.
/// </summary>
public sealed partial class PythonFrontEnd
{
    private readonly IReadOnlyList<Token> _tokens;
    private readonly ScriptError? _tokenizeError;
    private int _position;

    private PythonFrontEnd(TokenizeResult tokenized)
    {
        _tokens = tokenized.Tokens;
        _tokenizeError = tokenized.Error;
    }

    private Token Current
    {
        get
        {
            Token token = _tokens[_position];
            if (token.Kind == TokenKind.EndOfFile && _tokenizeError is { } error)
            {
                throw new ParseFailure(error);
            }

            return token;
        }
    }

    /// <summary>
    /// Parses Python source text into a program, or returns the first error in the script in source order.
    /// </summary>
    /// <param name="source">The script text, with any line break form.</param>
    /// <returns>A result holding the program with <see cref="ScriptLanguage.Python"/>, or the first error in the script.</returns>
    public static ParseResult Parse(string source)
    {
        var parser = new PythonFrontEnd(PythonTokenizer.Tokenize(source));
        try
        {
            var body = new List<Statement>();
            while (parser.Current.Kind != TokenKind.EndOfFile)
            {
                parser.ParseStatement(body);
            }

            return ParseResult.Success(new ScriptProgram(body, ScriptLanguage.Python));
        }
        catch (ParseFailure failure)
        {
            return ParseResult.Failure(failure.Error);
        }
    }

    private void ParseStatement(List<Statement> into)
    {
        Token first = Current;
        if (first.Kind == TokenKind.Keyword && first.Text == "pass")
        {
            Advance();
            Expect(TokenKind.Newline, string.Empty);
            return;
        }

        Expression expression = ParseExpression();
        Expect(TokenKind.Newline, string.Empty);
        into.Add(new ExpressionStatement(expression.Line, expression));
    }

    private Token Advance()
    {
        Token token = Current;
        _position++;
        return token;
    }

    private void Expect(TokenKind kind, string text)
    {
        Token token = Current;
        if (token.Kind != kind || token.Text != text)
        {
            throw UnexpectedToken(token);
        }

        Advance();
    }

    private bool IsOperator(string text)
    {
        return Current is { Kind: TokenKind.Operator } token && token.Text == text;
    }

    private static ParseFailure UnexpectedToken(Token token)
    {
        return Failure(ScriptErrorKind.SyntaxError, token.Line, "invalid syntax");
    }

    private static ParseFailure Failure(ScriptErrorKind kind, int line, string detail)
    {
        return new ParseFailure(new ScriptError(kind, line, detail));
    }

    private sealed class ParseFailure(ScriptError error) : Exception
    {
        public ScriptError Error { get; } = error;
    }
}
