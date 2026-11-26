using CodingGame.Core.Rules;
using Godot;

namespace CodingGame.Godot;

/// <summary>
/// The bar at the top left of the screen: one pill per item the drone holds, with a coloured icon and the count.
/// It only shows; it changes nothing.
/// </summary>
public partial class ResourceBar : HBoxContainer
{
    private Dictionary<string, int>? _shownItems;

    /// <summary>
    /// Shows the items as pills, each with the colour of its crop and a tooltip with its name. Items that are not a crop
    /// are white. With no items, one pill says "No items yet". The pills are rebuilt only when the items changed.
    /// </summary>
    /// <param name="items">The items the drone holds, keyed by item name. It is only read.</param>
    /// <param name="crops">The crops of the game rules. An item named like a crop gets that crop's colour.</param>
    public void ShowItems(IReadOnlyDictionary<string, int> items, IReadOnlyList<Crop> crops)
    {
        if (_shownItems is not null && SameItems(items, _shownItems))
        {
            return;
        }

        _shownItems = new Dictionary<string, int>(items);
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        if (items.Count == 0)
        {
            PanelContainer emptyPill = new();
            emptyPill.AddChild(BigLabel("No items yet"));
            AddChild(emptyPill);
            return;
        }

        foreach ((string name, int count) in items)
        {
            Crop? crop = crops.FirstOrDefault(entry => entry.Name == name);
            Color color = crop is null ? Colors.White : FieldView3D.CropColor(crop, crops);
            Control icon = new() { CustomMinimumSize = new Vector2(20, 20) };
            icon.Draw += () => icon.DrawCircle(new Vector2(10, 10), 10, color);

            HBoxContainer content = new();
            content.AddChild(icon);
            content.AddChild(BigLabel($"{count}"));

            PanelContainer pill = new() { TooltipText = name };
            pill.AddChild(content);
            AddChild(pill);
        }
    }

    private static bool SameItems(IReadOnlyDictionary<string, int> items, Dictionary<string, int> shownItems)
    {
        return items.Count == shownItems.Count
            && items.All(item => shownItems.TryGetValue(item.Key, out int count) && count == item.Value);
    }

    private static Label BigLabel(string text)
    {
        Label label = new() { Text = text };
        label.AddThemeFontSizeOverride("font_size", 20);
        return label;
    }
}
