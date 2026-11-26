using CodingGame.Core.Rules;
using Godot;

namespace CodingGame.Godot;

/// <summary>
/// The bar at the top left of the screen: one pill per item the drone holds, with a small drawing of the crop and the count.
/// It only shows; it changes nothing.
/// </summary>
public partial class ResourceBar : HBoxContainer
{
    private const int IconSize = 24;

    private Dictionary<string, int>? _shownItems;

    /// <summary>
    /// Shows the items as pills, each with a drawing of its crop in the crop's shape and colour, and a tooltip with its name.
    /// Items that are not a crop get a plain dot. With no items, one pill says "No items yet". The pills are rebuilt only when the items changed.
    /// </summary>
    /// <param name="items">The items the drone holds, keyed by item name. It is only read.</param>
    /// <param name="crops">The crops of the game rules. An item named like a crop gets that crop's shape and colour.</param>
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
            Control icon = new() { CustomMinimumSize = new Vector2(IconSize, IconSize) };
            icon.Draw += () => DrawItemIcon(icon, crop, crops);

            HBoxContainer content = new();
            content.AddChild(icon);
            content.AddChild(BigLabel($"{count}"));

            PanelContainer pill = new() { TooltipText = name };
            pill.AddChild(content);
            AddChild(pill);
        }
    }

    // A small drawing of the crop as it looks on the field, so the pill and the field read alike.
    private static void DrawItemIcon(Control icon, Crop? crop, IReadOnlyList<Crop> crops)
    {
        Vector2 centre = new(IconSize / 2f, IconSize / 2f);
        if (crop is null)
        {
            icon.DrawCircle(centre, 8, Palette.UiText);
            return;
        }

        Color color = Palette.CropColor(crop, crops);
        switch (FieldView3D.ShapeOf(crop, crops))
        {
            case CropShape.Leaves:
                foreach (float angleDegrees in new[] { -90f, 30f, 150f })
                {
                    icon.DrawCircle(centre + Vector2.FromAngle(Mathf.DegToRad(angleDegrees)) * 5.5f, 5.5f, color);
                }

                icon.DrawCircle(centre, 2.5f, Palette.CloverFlower);
                break;
            case CropShape.Bulb:
                icon.DrawCircle(new Vector2(8.5f, 6), 4, Palette.Leaf);
                icon.DrawCircle(new Vector2(15.5f, 6), 4, Palette.Leaf);
                icon.DrawColoredPolygon([new Vector2(10, 19), new Vector2(14, 19), new Vector2(12, 24)], color);
                icon.DrawCircle(new Vector2(12, 14), 7, color);
                break;
            case CropShape.Tubers:
                icon.DrawSetTransform(new Vector2(12, 13), 0.35f, new Vector2(1.3f, 1f));
                icon.DrawCircle(Vector2.Zero, 8, color);
                icon.DrawSetTransform(Vector2.Zero);
                icon.DrawCircle(new Vector2(9, 11), 1.5f, color.Darkened(0.3f));
                icon.DrawCircle(new Vector2(15, 15), 1.5f, color.Darkened(0.3f));
                break;
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
