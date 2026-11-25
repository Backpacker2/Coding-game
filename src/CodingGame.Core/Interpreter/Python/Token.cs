namespace CodingGame.Core.Interpreter.Python;

/// <summary>
/// The kind of a token in Python source.
/// </summary>
internal enum TokenKind
{
    /// <summary>An identifier that is not a keyword, such as a command or a constant name.</summary>
    Name,

    /// <summary>One of the Python keywords.</summary>
    Keyword,

    /// <summary>A run of digits.</summary>
    Number,

    /// <summary>A punctuation mark or an operator.</summary>
    Operator,

    /// <summary>The end of a statement line.</summary>
    Newline,

    /// <summary>A block that starts with a deeper indentation.</summary>
    Indent,

    /// <summary>The end of a block that is indented deeper.</summary>
    Dedent,

    /// <summary>The end of the source. Always the last token.</summary>
    EndOfFile,
}

/// <summary>
/// A token of Python source, with the line it is on.
/// </summary>
/// <param name="Kind">The kind of token.</param>
/// <param name="Text">The identifier, keyword, digits or operator as written, or an empty string for a structural token.</param>
/// <param name="Line">The line in the script, counted from 1.</param>
internal readonly record struct Token(TokenKind Kind, string Text, int Line);

/// <summary>
/// The result of tokenizing: the tokens up to the first problem, and the error if there was one.
/// </summary>
/// <param name="Tokens">The tokens, always ending with an <see cref="TokenKind.EndOfFile"/> token.</param>
/// <param name="Error">The first error in the source, or null when the source is valid.</param>
internal readonly record struct TokenizeResult(IReadOnlyList<Token> Tokens, ScriptError? Error);
