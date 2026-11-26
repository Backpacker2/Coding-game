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
            NumberColor = Palette.SyntaxNumber,
            FunctionColor = Palette.SyntaxCommand,
            SymbolColor = Palette.SyntaxSymbol,
        };
        foreach (string keyword in keywords)
        {
            highlighter.AddKeywordColor(keyword, Palette.SyntaxKeyword);
        }

        foreach (string command in api.Commands.Keys)
        {
            highlighter.AddKeywordColor(command, Palette.SyntaxCommand);
        }

        foreach (string constant in api.Constants.Keys)
        {
            highlighter.AddKeywordColor(constant, Palette.SyntaxConstant);
        }

        highlighter.AddColorRegion("\"", "\"", Palette.SyntaxString);
        highlighter.AddColorRegion("'", "'", Palette.SyntaxString);
        if (language == ScriptLanguage.Python)
        {
            highlighter.AddColorRegion("#", "", Palette.SyntaxComment, true);
        }
        else
        {
            highlighter.AddColorRegion("//", "", Palette.SyntaxComment, true);
            highlighter.AddColorRegion("/*", "*/", Palette.SyntaxComment);
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
