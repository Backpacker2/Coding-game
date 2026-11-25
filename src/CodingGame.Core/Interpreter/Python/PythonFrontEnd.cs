namespace CodingGame.Core.Interpreter.Python;

/// <summary>
/// Parses Python source into a <see cref="ScriptProgram"/>. Only the part of Python the game teaches is accepted;
/// anything else gives an error with a line number.
/// </summary>
public sealed partial class PythonFrontEnd
{
    // The parser recurses once per nested call or block. A limit turns a script nested
    // thousands of levels deep into an error instead of an uncatchable stack overflow.
    // Python itself stops at 200 nested parentheses and 100 nested blocks.
    private const int MaxNestingDepth = 100;

    private readonly IReadOnlyList<Token> _tokens;
    private readonly ScriptError? _tokenizeError;
    private int _position;
    private int _nestingDepth;

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
        switch (first)
        {
            case { Kind: TokenKind.Indent }:
                throw Failure(ScriptErrorKind.IndentationError, first.Line, "unexpected indent");
            case { Kind: TokenKind.Keyword, Text: "pass" }:
                Advance();
                Expect(TokenKind.Newline, string.Empty);
                return;
            case { Kind: TokenKind.Keyword, Text: "if" }:
                into.Add(ParseIf());
                return;
            case { Kind: TokenKind.Keyword, Text: "while" }:
                into.Add(ParseWhile());
                return;
            case { Kind: TokenKind.Keyword, Text: "for" }:
                into.Add(ParseFor());
                return;
            case { Kind: TokenKind.Keyword, Text: "def" }:
                into.Add(ParseFunctionDefinition());
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
        switch (token.Kind)
        {
            case TokenKind.Keyword when token.Text is not ("if" or "elif" or "else" or "while" or "for" or "in" or "def" or "pass" or "True" or "False"):
                return Failure(ScriptErrorKind.NotUnlocked, token.Line, token.Text);
            case TokenKind.Operator when token.Text == ")":
                return Failure(ScriptErrorKind.SyntaxError, token.Line, "unmatched ')'");
            case TokenKind.Operator when token.Text is not ("(" or "," or ":" or "==" or "!="):
                return Failure(ScriptErrorKind.NotUnlocked, token.Line, token.Text);
            default:
                return Failure(ScriptErrorKind.SyntaxError, token.Line, "invalid syntax");
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
