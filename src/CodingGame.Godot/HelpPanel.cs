using CodingGame.Core.Interpreter;
using Godot;

namespace CodingGame.Godot;

/// <summary>A scrollable text with the help entries the player can use now. It only shows; it changes nothing.</summary>
public partial class HelpPanel : ScrollContainer
{
    private static readonly (HelpKind Kind, string Heading)[] _groups =
    [
        (HelpKind.Lesson, "LESSONS"),
        (HelpKind.NextLesson, "NEXT"),
        (HelpKind.Command, "COMMANDS"),
        (HelpKind.Constant, "CONSTANTS"),
        (HelpKind.Keyword, "LANGUAGE"),
    ];

    private readonly RichTextLabel _text = new()
    {
        BbcodeEnabled = true,
        FitContent = true,
        ScrollActive = false,
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
    };

    /// <inheritdoc/>
    public override void _Ready()
    {
        StyleBoxEmpty padding = new();
        padding.SetContentMarginAll(14);
        _text.AddThemeStyleboxOverride("normal", padding);
        AddChild(_text);
    }

    /// <summary>
    /// Replaces the text with the given entries, grouped under coloured headings: Lessons, Next, Commands,
    /// Constants and Language. Examples are shown in the colour of code; the next lesson is greyed out, because it is not unlocked yet.
    /// </summary>
    /// <param name="entries">The entries to show, in order.</param>
    public void ShowEntries(IReadOnlyList<HelpEntry> entries)
    {
        List<string> groups = [];
        foreach ((HelpKind kind, string heading) in _groups)
        {
            List<string> lines = entries.Where(entry => entry.Kind == kind).Select(FormatEntry).ToList();
            if (lines.Count > 0)
            {
                string headingColor = Palette.UiAccent.Lightened(0.35f).ToHtml();
                groups.Add($"[font_size=18][b][color=#{headingColor}]{heading}[/color][/b][/font_size]\n" + string.Join("\n\n", lines));
            }
        }

        _text.Text = string.Join("\n\n\n", groups);
    }

    private static string FormatEntry(HelpEntry entry)
    {
        // Examples take the command colour of the editor, so they read as code the player can type.
        string example = string.Join("\n", entry.Example.Split('\n').Select(Escape));
        string text = $"[font_size=17][b]{Escape(entry.Name)}[/b][/font_size]\n{Escape(entry.Summary)}\n"
            + $"[indent][color=#{Palette.SyntaxCommand.ToHtml()}]{example}[/color][/indent]";
        return entry.Kind == HelpKind.NextLesson ? $"[color=#{Palette.MutedText.ToHtml()}]{text}[/color]" : text;
    }

    // Help texts and examples contain brackets, such as lists in Python, which BBCode would read as tags.
    private static string Escape(string text) => text.Replace("[", "[lb]");
}
