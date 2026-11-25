namespace CodingGame.Core.Interpreter.JavaScript;

/// <summary>
/// Parses JavaScript source into a <see cref="ScriptProgram"/>. Only the part of JavaScript the game teaches is accepted;
/// anything else gives an error with a line number.
/// </summary>
public sealed partial class JavaScriptFrontEnd
{
    // The parser recurses once per nested call or block. A limit turns a script nested
    // thousands of levels deep into an error instead of an uncatchable stack overflow.
    // The limit is the game's own; it keeps the parser's stack safe.
    private const int MaxNestingDepth = 100;

    private readonly IReadOnlyList<Token> _tokens;
    private readonly ScriptError? _tokenizeError;
    private int _position;
    private int _nestingDepth;

    private JavaScriptFrontEnd(TokenizeResult tokenized)
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
    /// Parses JavaScript source text into a program, or returns the first error in the script in source order.
    /// </summary>
    /// <param name="source">The script text, with any line break form.</param>
    /// <returns>A result holding the program with <see cref="ScriptLanguage.JavaScript"/>, or the first error in the script.</returns>
    public static ParseResult Parse(string source)
    {
        var parser = new JavaScriptFrontEnd(JavaScriptTokenizer.Tokenize(source));
        try
        {
            var body = new List<Statement>();
            while (parser.Current.Kind != TokenKind.EndOfFile)
            {
                parser.ParseStatement(body);
            }

            return ParseResult.Success(new ScriptProgram(body, ScriptLanguage.JavaScript));
        }
        catch (ParseFailure failure)
        {
            return ParseResult.Failure(failure.Error);
        }
    }

    private void ParseStatement(List<Statement> into)
    {
        Expression expression = ParseExpression();
        ExpectSemicolon();
        into.Add(new ExpressionStatement(expression.Line, expression));
    }

    private Token Advance()
    {
        Token token = Current;
        _position++;
        return token;
    }

    private bool IsOperator(string text)
    {
        return Current is { Kind: TokenKind.Operator } token && token.Text == text;
    }

    private void Expect(string operatorText)
    {
        if (!IsOperator(operatorText))
        {
            throw UnexpectedToken(Current);
        }

        Advance();
    }

    private void ExpectSemicolon()
    {
        Token token = Current;
        if (IsOperator(";"))
        {
            Advance();
            return;
        }

        // A statement may end without ';' only before a token on a later line, so the
        // error is reported on the line where the statement ends.
        Token previous = _tokens[_position - 1];
        if (token.Kind == TokenKind.EndOfFile || token.Line > previous.Line)
        {
            throw Failure(ScriptErrorKind.SyntaxError, previous.Line, "expected ';'");
        }

        throw UnexpectedToken(token);
    }

    private void EnterNesting(int line)
    {
        _nestingDepth++;
        if (_nestingDepth > MaxNestingDepth)
        {
            throw Failure(ScriptErrorKind.SyntaxError, line, "too many nested levels");
        }
    }

    private static ParseFailure UnexpectedToken(Token token)
    {
        switch (token)
        {
            case { Kind: TokenKind.Keyword, Text: not ("if" or "else" or "while" or "for" or "function" or "true" or "false") }:
                return Failure(ScriptErrorKind.NotUnlocked, token.Line, token.Text);
            case { Kind: TokenKind.Operator, Text: "==" }:
                return Failure(ScriptErrorKind.SyntaxError, token.Line, "'==' is not allowed, use '===': '==' converts types before it compares");
            case { Kind: TokenKind.Operator, Text: "!=" }:
                return Failure(ScriptErrorKind.SyntaxError, token.Line, "'!=' is not allowed, use '!==': '!=' converts types before it compares");
            case { Kind: TokenKind.Operator, Text: not ("(" or ")" or "{" or "}" or "," or ";" or "===" or "!==") }:
                return Failure(ScriptErrorKind.NotUnlocked, token.Line, token.Text);
            case { Kind: TokenKind.EndOfFile }:
                return Failure(ScriptErrorKind.SyntaxError, token.Line, "Unexpected end of input");
            case { Kind: TokenKind.Name }:
                return Failure(ScriptErrorKind.SyntaxError, token.Line, $"Unexpected identifier '{token.Text}'");
            case { Kind: TokenKind.Number }:
                return Failure(ScriptErrorKind.SyntaxError, token.Line, "Unexpected number");
            default:
                return Failure(ScriptErrorKind.SyntaxError, token.Line, $"Unexpected token '{token.Text}'");
        }
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
