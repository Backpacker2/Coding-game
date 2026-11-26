using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using Godot;

namespace CodingGame.Godot;

/// <summary>
/// Shows the player's next goal under the item bar: the first unlock in the game's order that is not bought yet, its price and a bar with how much of the price item the player has. When it can be bought the bar is full and the text says so. With every unlock bought it hides. It only reads the progress.
/// </summary>
public partial class GoalBar : PanelContainer
{
    private readonly Label _titleLabel = new();
    private readonly ProgressBar _bar = new() { ShowPercentage = false, CustomMinimumSize = new Vector2(240, 10) };
    private readonly StyleBoxFlat _fill = new();

    /// <summary>
    /// Builds the title and the bar. The bar uses the colour of the price crop and a dark background.
    /// </summary>
    public GoalBar()
    {
        _fill.SetCornerRadiusAll(4);
        StyleBoxFlat background = new() { BgColor = Palette.GoalBarBackground };
        background.SetCornerRadiusAll(4);
        _bar.AddThemeStyleboxOverride("fill", _fill);
        _bar.AddThemeStyleboxOverride("background", background);

        _titleLabel.AddThemeFontSizeOverride("font_size", 16);
        VBoxContainer content = new();
        content.AddChild(_titleLabel);
        content.AddChild(_bar);
        AddChild(content);
    }

    /// <summary>
    /// Shows the first unlock that is not bought yet, or hides the bar when every unlock is bought.
    /// </summary>
    /// <param name="progress">The progress to read the next unlock and the items from. It is not changed.</param>
    public void ShowNextGoal(GameProgress progress)
    {
        Unlock? next = progress.Rules.Unlocks.FirstOrDefault(unlock => !progress.IsBought(unlock));
        if (next is null)
        {
            Visible = false;
            return;
        }

        int have = progress.Items.GetValueOrDefault(next.Price.Item);
        int need = next.Price.Count;
        _bar.MaxValue = need;
        _bar.Value = Math.Min(have, need);
        Crop? crop = progress.Rules.Crops.FirstOrDefault(c => c.Name == next.Price.Item);
        _fill.BgColor = crop is null ? Palette.MutedText : Palette.CropColor(crop, progress.Rules.Crops);
        _titleLabel.Text = progress.CanBuy(next)
            ? $"Next: {next.Title} - ready, open Unlocks"
            : $"Next: {next.Title} - {Math.Min(have, need)} / {need} {next.Price.Item.ToLowerInvariant()}";
        Visible = true;
    }
}
