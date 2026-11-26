using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.JavaScript;
using CodingGame.Core.Interpreter.Python;
using Godot;
using ScriptLanguage = CodingGame.Core.Interpreter.ScriptLanguage;

namespace CodingGame.Godot;

/// <summary>
/// The script text box with line numbers, one highlighted line and syntax colours for the selected language.
/// </summary>
public partial class ScriptEditor : CodeEdit
{
    // Bright enough to read on the dark editor background.
    private static readonly Color _keywordColor = new(0.8f, 0.47f, 0.87f);
    private static readonly Color _commandColor = new(0.4f, 0.73f, 1f);
    private static readonly Color _constantColor = new(0.95f, 0.75f, 0.35f);
    private static readonly Color _numberColor = new(0.6f, 0.85f, 0.55f);
    private static readonly Color _stringColor = new(0.85f, 0.6f, 0.45f);
    private static readonly Color _commentColor = new(0.55f, 0.58f, 0.62f);
    private static readonly Color _symbolColor = new(0.85f, 0.85f, 0.85f);

    /// <summary>Creates an editor that shows line numbers and grows to fill its parent vertically.</summary>
    public ScriptEditor()
    {
        GuttersDrawLineNumbers = true;
        SizeFlagsVertical = SizeFlags.ExpandFill;
    }

    /// <summary>Colours the keywords, drone commands, constants, numbers, strings and comments of a language.</summary>
    /// <param name="language">The language the editor now shows.</param>
    /// <param name="api">The drone API whose command and constant names are coloured.</param>
    /// <exception cref="ArgumentOutOfRangeException">The language is not one the game knows.</exception>
    public void UseLanguage(ScriptLanguage language, DroneApi api)
    {
        IReadOnlySet<string> keywords = language switch
        {
            ScriptLanguage.Python => PythonFrontEnd.Keywords,
            ScriptLanguage.JavaScript => JavaScriptFrontEnd.Keywords,
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, "unknown script language"),
        };

        // A function call uses the command colour, so a player's own call looks like a drone command.
        // Symbols need their own colour: the highlighter's default is black, which hides brackets on the dark background.
        CodeHighlighter highlighter = new()
        {
            NumberColor = _numberColor,
            FunctionColor = _commandColor,
            SymbolColor = _symbolColor,
        };
        foreach (string keyword in keywords)
        {
            highlighter.AddKeywordColor(keyword, _keywordColor);
        }

        foreach (string command in api.Commands.Keys)
        {
            highlighter.AddKeywordColor(command, _commandColor);
        }

        foreach (string constant in api.Constants.Keys)
        {
            highlighter.AddKeywordColor(constant, _constantColor);
        }

        highlighter.AddColorRegion("\"", "\"", _stringColor);
        highlighter.AddColorRegion("'", "'", _stringColor);
        if (language == ScriptLanguage.Python)
        {
            highlighter.AddColorRegion("#", "", _commentColor, true);
        }
        else
        {
            highlighter.AddColorRegion("//", "", _commentColor, true);
            highlighter.AddColorRegion("/*", "*/", _commentColor);
        }

        SyntaxHighlighter = highlighter;
    }

    /// <summary>
    /// Highlights one line in the given colour and removes any earlier highlight.
    /// A line below 1 only clears the highlight.
    /// </summary>
    /// <param name="line">The line to highlight, counting from 1.</param>
    /// <param name="color">The background colour of the line.</param>
    public void HighlightLine(int line, Color color)
    {
        ClearHighlight();
        if (line < 1)
        {
            return;
        }

        // An error at the end of the script can point one line past the last line.
        SetLineBackgroundColor(Math.Min(line, GetLineCount()) - 1, color);
    }

    /// <summary>Removes the background colour from every line.</summary>
    public void ClearHighlight()
    {
        for (int line = 0; line < GetLineCount(); line++)
        {
            SetLineBackgroundColor(line, Colors.Transparent);
        }
    }
}
