namespace CodingGame.Core.Interpreter.JavaScript;

/// <summary>
/// Turns JavaScript source text into tokens. Comments and white space produce no tokens.
/// </summary>
internal static class JavaScriptTokenizer
{
    private static readonly HashSet<string> _keywords =
    [
        "async", "await", "break", "case", "catch", "class", "const", "continue", "debugger", "default", "delete",
        "do", "else", "enum", "export", "extends", "false", "finally", "for", "function", "if", "import", "in",
        "instanceof", "let", "new", "null", "return", "super", "switch", "this", "throw", "true", "try", "typeof",
        "var", "void", "while", "with", "yield",
    ];

    // Longest first, so that "===" is not read as "==" followed by "=".
    private static readonly string[] _multiCharacterOperators =
        ["===", "!==", "==", "!=", "<=", ">=", "++", "--", "+=", "-=", "*=", "/=", "%=", "&&", "||", "=>", "**"];

    private static readonly string _singleCharacterOperators = "()[]{},:;.+-*/%<>=&|^~?!";

    /// <summary>
    /// Tokenizes JavaScript source. Every line break form (LF, CRLF and CR) ends a line.
    /// </summary>
    /// <param name="source">The script text.</param>
    /// <returns>
    /// The tokens, ending with an end-of-file token, and the first error in the script or null.
    /// After an error the tokens stop before the problem, and the end-of-file token is on the line of the error.
    /// </returns>
    internal static TokenizeResult Tokenize(string source)
    {
        var tokens = new List<Token>();
        var error = ScanSource(source.Replace("\r\n", "\n").Replace('\r', '\n'), tokens);
        var endLine = error?.Line ?? (tokens.Count == 0 ? 1 : tokens[^1].Line);
        tokens.Add(new Token(TokenKind.EndOfFile, string.Empty, endLine));
        return new TokenizeResult(tokens, error);
    }

    private static ScriptError? ScanSource(string text, List<Token> into)
    {
        var line = 1;
        var index = 0;
        while (index < text.Length)
        {
            var character = text[index];
            if (character == '\n')
            {
                line++;
                index++;
            }
            else if (character is ' ' or '\t')
            {
                index++;
            }
            else if (character == '/' && index + 1 < text.Length && text[index + 1] == '/')
            {
                var lineEnd = text.IndexOf('\n', index);
                index = lineEnd < 0 ? text.Length : lineEnd;
            }
            else if (character == '/' && index + 1 < text.Length && text[index + 1] == '*')
            {
                return new ScriptError(ScriptErrorKind.NotUnlocked, line, "block comments");
            }
            else if (character is '\'' or '"' or '`')
            {
                return new ScriptError(ScriptErrorKind.NotUnlocked, line, "strings");
            }
            else if (IsIdentifierStart(character))
            {
                var end = index + 1;
                while (end < text.Length && IsIdentifierPart(text[end]))
                {
                    end++;
                }

                var word = text[index..end];
                var kind = _keywords.Contains(word) ? TokenKind.Keyword : TokenKind.Name;
                into.Add(new Token(kind, word, line));
                index = end;
            }
            else if (char.IsAsciiDigit(character))
            {
                var end = index + 1;
                while (end < text.Length && char.IsAsciiDigit(text[end]))
                {
                    end++;
                }

                var error = CheckNumber(text, index, end, line);
                if (error is not null)
                {
                    return error;
                }

                into.Add(new Token(TokenKind.Number, text[index..end], line));
                index = end;
            }
            else
            {
                var operatorText = OperatorAt(text, index);
                if (operatorText is null)
                {
                    return new ScriptError(ScriptErrorKind.SyntaxError, line, $"invalid character '{character}'");
                }

                into.Add(new Token(TokenKind.Operator, operatorText, line));
                index += operatorText.Length;
            }
        }

        return null;
    }

    private static ScriptError? CheckNumber(string text, int start, int end, int line)
    {
        var next = end < text.Length ? text[end] : '\0';
        if (next == '.')
        {
            return new ScriptError(ScriptErrorKind.NotUnlocked, line, "decimal numbers");
        }

        if (IsIdentifierStart(next))
        {
            return new ScriptError(ScriptErrorKind.SyntaxError, line, "Invalid or unexpected token");
        }

        var digits = text[start..end];
        if (digits.Length > 1 && digits[0] == '0')
        {
            return new ScriptError(ScriptErrorKind.SyntaxError, line, "leading zeros are not allowed in numbers");
        }

        if (!int.TryParse(digits, out _))
        {
            return new ScriptError(ScriptErrorKind.SyntaxError, line, "number is too large");
        }

        return null;
    }

    private static bool IsIdentifierStart(char character)
    {
        return char.IsAsciiLetter(character) || character is '_' or '$';
    }

    private static bool IsIdentifierPart(char character)
    {
        return IsIdentifierStart(character) || char.IsAsciiDigit(character);
    }

    private static string? OperatorAt(string text, int index)
    {
        foreach (var candidate in _multiCharacterOperators)
        {
            if (string.CompareOrdinal(text, index, candidate, 0, candidate.Length) == 0)
            {
                return candidate;
            }
        }

        return _singleCharacterOperators.Contains(text[index]) ? text[index].ToString() : null;
    }
}
