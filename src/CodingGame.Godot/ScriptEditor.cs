using Godot;

namespace CodingGame.Godot;

/// <summary>The script text box with line numbers and one highlighted line.</summary>
public partial class ScriptEditor : CodeEdit
{
    /// <summary>Creates an editor that shows line numbers and grows to fill its parent vertically.</summary>
    public ScriptEditor()
    {
        GuttersDrawLineNumbers = true;
        SizeFlagsVertical = SizeFlags.ExpandFill;
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
