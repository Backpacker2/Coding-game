using CodingGame.Core.Interpreter;
using Godot;

namespace CodingGame.Godot;

/// <summary>A scrollable text with the help entries the player can use now. It only shows; it changes nothing.</summary>
public partial class HelpPanel : ScrollContainer
{
    private static readonly (HelpKind Kind, string Heading)[] _groups =
    [
        (HelpKind.Command, "COMMANDS"),
        (HelpKind.Constant, "CONSTANTS"),
        (HelpKind.Keyword, "LANGUAGE"),
    ];

    private readonly Label _text = new()
    {
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
    };

    /// <inheritdoc/>
    public override void _Ready()
    {
        AddChild(_text);
    }

    /// <summary>Replaces the text with the given entries, grouped as Commands, Constants and Language.</summary>
    /// <param name="entries">The entries to show, in order.</param>
    public void ShowEntries(IReadOnlyList<HelpEntry> entries)
    {
        List<string> groups = [];
        foreach ((HelpKind kind, string heading) in _groups)
        {
            List<string> lines = entries.Where(entry => entry.Kind == kind).Select(FormatEntry).ToList();
            if (lines.Count > 0)
            {
                groups.Add(heading + "\n" + string.Join("\n", lines));
            }
        }

        _text.Text = string.Join("\n\n", groups);
    }

    private static string FormatEntry(HelpEntry entry)
    {
        string example = string.Join("\n", entry.Example.Split('\n').Select(line => "    " + line));
        return $"{entry.Name}: {entry.Summary}\n{example}";
    }
}
