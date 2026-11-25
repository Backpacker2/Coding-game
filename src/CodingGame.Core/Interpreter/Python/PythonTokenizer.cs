namespace CodingGame.Core.Interpreter.Python;

/// <summary>
/// Turns Python source text into tokens. Indentation becomes <see cref="TokenKind.Indent"/> and
/// <see cref="TokenKind.Dedent"/> tokens, and comments and blank lines produce no tokens.
/// </summary>
internal static class PythonTokenizer
{
    private static readonly HashSet<string> _keywords =
    [
        "False", "None", "True", "and", "as", "assert", "async", "await", "break", "class", "continue",
        "def", "del", "elif", "else", "except", "finally", "for", "from", "global", "if", "import", "in",
        "is", "lambda", "nonlocal", "not", "or", "pass", "raise", "return", "try", "while", "with", "yield",
    ];

    private static readonly string[] _twoCharacterOperators =
        ["==", "!=", "<=", ">=", "+=", "-=", "*=", "/=", "%=", "//", "**", "->", ":="];

    /// <summary>
    /// Tokenizes Python source. Every line break form (LF, CRLF and CR) ends a line.
    /// </summary>
    /// <param name="source">The script text.</param>
    /// <returns>The tokens, ending with an end-of-file token, and no error.</returns>
    internal static TokenizeResult Tokenize(string source)
    {
        var tokens = new List<Token>();
        var indentWidths = new List<int> { 0 };
        var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var lineNumber = index + 1;
            var line = lines[index];
            var contentStart = 0;
            while (contentStart < line.Length && line[contentStart] == ' ')
            {
                contentStart++;
            }

            if (contentStart == line.Length || line[contentStart] == '#')
            {
                continue;
            }

            if (contentStart > indentWidths[^1])
            {
                indentWidths.Add(contentStart);
                tokens.Add(new Token(TokenKind.Indent, string.Empty, lineNumber));
            }
            else
            {
                while (contentStart < indentWidths[^1])
                {
                    indentWidths.RemoveAt(indentWidths.Count - 1);
                    tokens.Add(new Token(TokenKind.Dedent, string.Empty, lineNumber));
                }
            }

            ScanLine(line, contentStart, lineNumber, tokens);
            tokens.Add(new Token(TokenKind.Newline, string.Empty, lineNumber));
        }

        var endLine = tokens.Count == 0 ? 1 : tokens[^1].Line;
        for (var level = indentWidths.Count; level > 1; level--)
        {
            tokens.Add(new Token(TokenKind.Dedent, string.Empty, endLine));
        }

        tokens.Add(new Token(TokenKind.EndOfFile, string.Empty, endLine));
        return new TokenizeResult(tokens, null);
    }

    private static void ScanLine(string line, int start, int lineNumber, List<Token> into)
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
                return;
            }
            else if (char.IsAsciiLetter(character) || character == '_')
            {
                var end = index + 1;
                while (end < line.Length && (char.IsAsciiLetterOrDigit(line[end]) || line[end] == '_'))
                {
                    end++;
                }

                var word = line[index..end];
                var kind = _keywords.Contains(word) ? TokenKind.Keyword : TokenKind.Name;
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

                into.Add(new Token(TokenKind.Number, line[index..end], lineNumber));
                index = end;
            }
            else
            {
                var length = index + 1 < line.Length && _twoCharacterOperators.Contains(line.Substring(index, 2)) ? 2 : 1;
                into.Add(new Token(TokenKind.Operator, line.Substring(index, length), lineNumber));
                index += length;
            }
        }
    }
}
