namespace CodingGame.Core.Interpreter.Python;

/// <summary>
/// Turns Python source text into tokens. Indentation becomes <see cref="TokenKind.Indent"/> and
/// <see cref="TokenKind.Dedent"/> tokens, and comments and blank lines produce no tokens.
/// </summary>
internal static class PythonTokenizer
{
    internal static readonly HashSet<string> Keywords =
    [
        "False", "None", "True", "and", "as", "assert", "async", "await", "break", "class", "continue",
        "def", "del", "elif", "else", "except", "finally", "for", "from", "global", "if", "import", "in",
        "is", "lambda", "nonlocal", "not", "or", "pass", "raise", "return", "try", "while", "with", "yield",
    ];

    private static readonly string[] _twoCharacterOperators =
        ["==", "!=", "<=", ">=", "+=", "-=", "*=", "/=", "%=", "//", "**", "->", ":="];

    private static readonly string _singleCharacterOperators = "()[]{},:;.+-*/%<>=&|^~@";

    /// <summary>
    /// Tokenizes Python source. Every line break form (LF, CRLF and CR) ends a line.
    /// </summary>
    /// <param name="source">The script text.</param>
    /// <returns>
    /// The tokens, ending with an end-of-file token, and the first error in the script or null.
    /// After an error the tokens stop before the problem, and the end-of-file token is on the line of the error.
    /// </returns>
    internal static TokenizeResult Tokenize(string source)
    {
        var tokens = new List<Token>();
        var indentWidths = new List<int> { 0 };
        var error = TokenizeLines(source, tokens, indentWidths);
        var endLine = error?.Line ?? (tokens.Count == 0 ? 1 : tokens[^1].Line);

        if (error is null)
        {
            for (var level = indentWidths.Count; level > 1; level--)
            {
                tokens.Add(new Token(TokenKind.Dedent, string.Empty, endLine));
            }
        }

        tokens.Add(new Token(TokenKind.EndOfFile, string.Empty, endLine));
        return new TokenizeResult(tokens, error);
    }

    private static ScriptError? TokenizeLines(string source, List<Token> tokens, List<int> indentWidths)
    {
        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        char? indentCharacter = null;

        for (var index = 0; index < lines.Length; index++)
        {
            var lineNumber = index + 1;
            var line = lines[index];
            var contentStart = 0;
            while (contentStart < line.Length && (line[contentStart] is ' ' or '\t'))
            {
                contentStart++;
            }

            if (contentStart == line.Length || line[contentStart] == '#')
            {
                continue;
            }

            var indentation = line[..contentStart];
            if (indentation.Length > 0)
            {
                var indentError = CheckIndentationKind(indentation, lineNumber, ref indentCharacter);
                if (indentError is not null)
                {
                    return indentError;
                }
            }

            if (contentStart > indentWidths[^1])
            {
                indentWidths.Add(contentStart);
                tokens.Add(new Token(TokenKind.Indent, string.Empty, lineNumber));
            }
            else
            {
                var levels = indentWidths.Count;
                while (contentStart < indentWidths[levels - 1])
                {
                    levels--;
                }

                if (indentWidths[levels - 1] != contentStart)
                {
                    return new ScriptError(ScriptErrorKind.IndentationError, lineNumber, "unindent does not match any outer indentation level");
                }

                while (indentWidths.Count > levels)
                {
                    indentWidths.RemoveAt(indentWidths.Count - 1);
                    tokens.Add(new Token(TokenKind.Dedent, string.Empty, lineNumber));
                }
            }

            var error = ScanLine(line, contentStart, lineNumber, tokens);
            if (error is not null)
            {
                return error;
            }

            tokens.Add(new Token(TokenKind.Newline, string.Empty, lineNumber));
        }

        return null;
    }

    private static ScriptError? CheckIndentationKind(string indentation, int lineNumber, ref char? indentCharacter)
    {
        if (indentation.Any(character => character != indentation[0]))
        {
            return new ScriptError(ScriptErrorKind.IndentationError, lineNumber, "indentation mixes tabs and spaces; use one kind");
        }

        indentCharacter ??= indentation[0];
        if (indentation[0] == indentCharacter)
        {
            return null;
        }

        string thisLine = indentation[0] == '\t' ? "tabs" : "spaces";
        string earlierLines = indentCharacter == '\t' ? "tabs" : "spaces";
        return new ScriptError(
            ScriptErrorKind.IndentationError,
            lineNumber,
            $"this line is indented with {thisLine}, earlier lines use {earlierLines}; use one kind");
    }

    private static ScriptError? ScanLine(string line, int start, int lineNumber, List<Token> into)
    {
        var index = start;
        while (index < line.Length)
        {
            var character = line[index];
            if (character is ' ' or '\t')
            {
                index++;
            }
            else if (character == '#')
            {
                return null;
            }
            else if (char.IsAsciiLetter(character) || character == '_')
            {
                var end = index + 1;
                while (end < line.Length && (char.IsAsciiLetterOrDigit(line[end]) || line[end] == '_'))
                {
                    end++;
                }

                var word = line[index..end];
                var kind = Keywords.Contains(word) ? TokenKind.Keyword : TokenKind.Name;
                into.Add(new Token(kind, word, lineNumber));
                index = end;
            }
            else if (char.IsAsciiDigit(character))
            {
                var end = index + 1;
                while (end < line.Length && char.IsAsciiDigit(line[end]))
                {
                    end++;
                }

                var error = CheckNumber(line, index, end, lineNumber);
                if (error is not null)
                {
                    return error;
                }

                into.Add(new Token(TokenKind.Number, line[index..end], lineNumber));
                index = end;
            }
            else if (character is '\'' or '"')
            {
                return new ScriptError(ScriptErrorKind.NotInGame, lineNumber, "strings");
            }
            else
            {
                var length = index + 1 < line.Length && _twoCharacterOperators.Contains(line.Substring(index, 2)) ? 2 : 1;
                if (length == 1 && !_singleCharacterOperators.Contains(character))
                {
                    return new ScriptError(ScriptErrorKind.SyntaxError, lineNumber, $"invalid character '{character}'");
                }

                into.Add(new Token(TokenKind.Operator, line.Substring(index, length), lineNumber));
                index += length;
            }
        }

        return null;
    }

    private static ScriptError? CheckNumber(string line, int start, int end, int lineNumber)
    {
        var next = end < line.Length ? line[end] : '\0';
        if (next == '.')
        {
            return new ScriptError(ScriptErrorKind.NotInGame, lineNumber, "decimal numbers");
        }

        if (char.IsAsciiLetter(next) || next == '_')
        {
            return new ScriptError(ScriptErrorKind.SyntaxError, lineNumber, "invalid decimal literal");
        }

        var digits = line[start..end];
        if (digits.Length > 1 && digits[0] == '0')
        {
            return new ScriptError(ScriptErrorKind.SyntaxError, lineNumber, "leading zeros in decimal integer literals are not permitted");
        }

        if (!int.TryParse(digits, out _))
        {
            return new ScriptError(ScriptErrorKind.SyntaxError, lineNumber, "integer is too large");
        }

        return null;
    }
}
