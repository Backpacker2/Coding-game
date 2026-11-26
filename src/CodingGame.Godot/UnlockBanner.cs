using CodingGame.Core.Rules;
using Godot;

namespace CodingGame.Godot;

/// <summary>
/// A short banner over the field that celebrates a bought unlock: its title, what it adds and a Show me button that opens Help. It fades out by itself. It only shows; it changes nothing.
/// </summary>
public partial class UnlockBanner : PanelContainer
{
    private const float ShownSeconds = 4f;
    private const float FadeSeconds = 0.6f;

    private readonly Label _titleLabel = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly Label _featuresLabel = new() { HorizontalAlignment = HorizontalAlignment.Center };
    private Tween? _fade;

    /// <summary>Raised when the player presses Show me.</summary>
    public event Action? ShowMePressed;

    /// <summary>
    /// Builds the labels and the Show me button. The banner starts hidden.
    /// </summary>
    public UnlockBanner()
    {
        _titleLabel.AddThemeFontSizeOverride("font_size", 22);
        _titleLabel.AddThemeColorOverride("font_color", Palette.UnlockGlow);
        _featuresLabel.AddThemeFontSizeOverride("font_size", 14);
        _featuresLabel.AddThemeColorOverride("font_color", Palette.MutedText);

        Button showMeButton = new() { Text = "Show me" };
        showMeButton.Pressed += () =>
        {
            ShowMePressed?.Invoke();
            Visible = false;
        };
        HBoxContainer buttonRow = new() { Alignment = BoxContainer.AlignmentMode.Center };
        buttonRow.AddChild(showMeButton);

        VBoxContainer content = new();
        content.AddChild(_titleLabel);
        content.AddChild(_featuresLabel);
        content.AddChild(buttonRow);
        AddChild(content);

        StyleBoxFlat background = new() { BgColor = Palette.BannerBackground };
        background.SetCornerRadiusAll(8);
        background.SetContentMarginAll(12);
        AddThemeStyleboxOverride("panel", background);

        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
    }

    /// <summary>
    /// Shows the banner for a bought unlock, replacing an earlier banner that is still showing.
    /// </summary>
    /// <param name="unlock">The unlock that was bought. Its title and what it adds are shown.</param>
    public void ShowUnlock(Unlock unlock)
    {
        _titleLabel.Text = $"New: {unlock.Title}";
        string features = unlock.Features.Count > 0
            ? "You can now use " + string.Join(", ", unlock.Features)
            : unlock.FieldSize is { } size ? $"The field is now {size.Width}x{size.Height}" : "";
        _featuresLabel.Text = features;
        _featuresLabel.Visible = features.Length > 0;

        _fade?.Kill();
        Modulate = Colors.White;
        Visible = true;
        _fade = CreateTween();
        _fade.TweenInterval(ShownSeconds);
        _fade.TweenProperty(this, "modulate:a", 0f, FadeSeconds);
        _fade.TweenCallback(Callable.From(Hide));
    }
}
