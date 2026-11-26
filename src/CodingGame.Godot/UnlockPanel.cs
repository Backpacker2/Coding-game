using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using Godot;

namespace CodingGame.Godot;

/// <summary>
/// A list of the unlocks with their prices and a Buy button for each. It changes the progress only when a button is pressed.
/// </summary>
public partial class UnlockPanel : VBoxContainer
{
    private readonly List<UnlockRow> _rows = [];
    private GameProgress _progress = null!;

    /// <summary>Raised after an unlock was bought, with that unlock.</summary>
    public event Action<Unlock>? Bought;

    /// <summary>
    /// Builds one row per unlock of the rules: the title, the price and a Buy button.
    /// Call it once, because it adds the rows and does not remove earlier ones.
    /// </summary>
    /// <param name="progress">The progress that buying an unlock changes.</param>
    public void Show(GameProgress progress)
    {
        _progress = progress;
        foreach (Unlock unlock in progress.Rules.Unlocks)
        {
            Label title = new()
            {
                Text = $"{unlock.Title}: {unlock.Price.Count} {unlock.Price.Item}",
                TooltipText = TooltipFor(unlock),
                MouseFilter = Control.MouseFilterEnum.Stop,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            Button buyButton = new() { Text = "Buy" };
            buyButton.Pressed += () => OnBuy(unlock);

            HBoxContainer row = new();
            row.AddChild(title);
            row.AddChild(buyButton);
            AddChild(row);
            _rows.Add(new UnlockRow(unlock, buyButton));
        }
    }

    /// <summary>
    /// Updates the buttons: a bought unlock says Unlocked and has no active button. Any other button is
    /// enabled only when the player can afford the unlock and no script is running.
    /// </summary>
    /// <param name="scriptRunning">True while a script runs, because nothing can be bought then.</param>
    public void Refresh(bool scriptRunning)
    {
        foreach (UnlockRow row in _rows)
        {
            bool bought = _progress.IsBought(row.Unlock);
            row.BuyButton.Text = bought ? "Unlocked" : "Buy";
            row.BuyButton.Disabled = scriptRunning || !_progress.CanBuy(row.Unlock);
        }
    }

    private static string TooltipFor(Unlock unlock)
    {
        List<string> parts = [.. unlock.Features];
        if (unlock.FieldSize is { } size)
        {
            parts.Add($"field {size.Width}x{size.Height}");
        }

        return string.Join(", ", parts);
    }

    private void OnBuy(Unlock unlock)
    {
        _progress.Buy(unlock);
        Bought?.Invoke(unlock);
    }

    private sealed record UnlockRow(Unlock Unlock, Button BuyButton);
}
