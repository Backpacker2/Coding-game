using Godot;

namespace CodingGame.Godot;

/// <summary>
/// Builds the one Theme the whole game UI uses, so every window, button, tab, menu and scroll bar has the same look.
/// Colours come from <see cref="Palette"/>.
/// </summary>
public static class GameTheme
{
    /// <summary>
    /// Creates a flat box with rounded corners, the background of most UI parts.
    /// </summary>
    /// <param name="color">The background colour of the box.</param>
    /// <param name="cornerRadius">The radius of all four corners in pixels.</param>
    /// <returns>A new box that the caller may change further.</returns>
    public static StyleBoxFlat Box(Color color, int cornerRadius)
    {
        StyleBoxFlat box = new() { BgColor = color };
        box.SetCornerRadiusAll(cornerRadius);
        box.ContentMarginLeft = 10;
        box.ContentMarginRight = 10;
        box.ContentMarginTop = 6;
        box.ContentMarginBottom = 6;
        return box;
    }

    /// <summary>
    /// Gives one button its own colour in all states, for Run, Stop and the round buttons.
    /// </summary>
    /// <param name="button">The button to colour. Its other overrides are kept.</param>
    /// <param name="color">The colour of the button at rest. Hover and pressed states are derived from it.</param>
    public static void ColorButton(Button button, Color color)
    {
        button.AddThemeStyleboxOverride("normal", Box(color, 8));
        button.AddThemeStyleboxOverride("hover", Box(color.Lightened(0.12f), 8));
        button.AddThemeStyleboxOverride("pressed", Box(color.Darkened(0.15f), 8));
        button.AddThemeStyleboxOverride("disabled", Box(new Color(color, 0.4f), 8));
    }

    /// <summary>
    /// Builds the theme for the root window. Every control and window takes its look from this one object.
    /// </summary>
    /// <returns>A theme with the look of all game controls, with a default font size of 16.</returns>
    public static Theme Build()
    {
        Theme theme = new() { DefaultFontSize = 16 };

        foreach (string type in new[] { "Button", "OptionButton" })
        {
            theme.SetStylebox("normal", type, Box(Palette.UiButton, 8));
            theme.SetStylebox("hover", type, Box(Palette.UiButtonHover, 8));
            theme.SetStylebox("pressed", type, Box(Palette.UiButtonPressed, 8));
            theme.SetStylebox("disabled", type, Box(new Color(Palette.UiButton, 0.5f), 8));
            theme.SetStylebox("focus", type, new StyleBoxEmpty());
            theme.SetColor("font_color", type, Palette.UiText);
            theme.SetColor("font_hover_color", type, Palette.UiText);
            theme.SetColor("font_pressed_color", type, Palette.UiText);
            theme.SetColor("font_focus_color", type, Palette.UiText);
            theme.SetColor("font_disabled_color", type, new Color(Palette.UiText, 0.4f));
            theme.SetColor("icon_normal_color", type, Palette.UiText);
            theme.SetColor("icon_hover_color", type, Palette.UiText);
            theme.SetColor("icon_pressed_color", type, Palette.UiText);
            theme.SetColor("icon_focus_color", type, Palette.UiText);
            theme.SetColor("icon_disabled_color", type, new Color(Palette.UiText, 0.4f));
        }

        theme.SetStylebox("panel", "PanelContainer", Box(Palette.UiPanel, 10));
        theme.SetStylebox("panel", "Panel", Box(Palette.UiPanel, 10));

        StyleBoxFlat popupPanel = Box(Palette.UiPanel, 8);
        popupPanel.SetBorderWidthAll(1);
        popupPanel.BorderColor = Palette.UiPanelBorder;
        theme.SetStylebox("panel", "PopupMenu", popupPanel);
        theme.SetStylebox("hover", "PopupMenu", Box(Palette.UiButtonHover, 6));
        theme.SetColor("font_color", "PopupMenu", Palette.UiText);
        theme.SetColor("font_hover_color", "PopupMenu", Colors.White);
        theme.SetColor("font_separator_color", "PopupMenu", new Color(Palette.UiText, 0.6f));

        StyleBoxFlat windowBorder = Box(Palette.UiPanel, 10);
        windowBorder.SetBorderWidthAll(1);
        windowBorder.BorderColor = Palette.UiPanelBorder;
        windowBorder.ExpandMarginTop = 32;
        windowBorder.ExpandMarginLeft = 4;
        windowBorder.ExpandMarginRight = 4;
        windowBorder.ExpandMarginBottom = 4;
        windowBorder.ShadowSize = 14;
        windowBorder.ShadowColor = new Color(0, 0, 0, 0.35f);
        theme.SetStylebox("embedded_border", "Window", windowBorder);
        theme.SetStylebox("embedded_unfocused_border", "Window", windowBorder);
        theme.SetColor("title_color", "Window", Palette.UiText);
        theme.SetConstant("title_height", "Window", 32);
        theme.SetFontSize("title_font_size", "Window", 16);

        theme.SetStylebox("panel", "AcceptDialog", Box(Palette.UiPanel, 0));
        theme.SetStylebox("panel", "ConfirmationDialog", Box(Palette.UiPanel, 0));

        theme.SetStylebox("tab_selected", "TabBar", TabBox(Palette.UiButtonHover));
        theme.SetStylebox("tab_unselected", "TabBar", TabBox(Palette.UiButton));
        theme.SetStylebox("tab_hovered", "TabBar", TabBox(Palette.UiButtonHover));
        theme.SetStylebox("tab_disabled", "TabBar", TabBox(new Color(Palette.UiButton, 0.5f)));
        theme.SetColor("font_selected_color", "TabBar", Palette.UiText);
        theme.SetColor("font_unselected_color", "TabBar", new Color(Palette.UiText, 0.7f));
        theme.SetColor("font_hovered_color", "TabBar", Palette.UiText);
        theme.SetColor("font_disabled_color", "TabBar", new Color(Palette.UiText, 0.35f));

        StyleBoxFlat focusedEdit = Box(Palette.UiEditor, 6);
        focusedEdit.SetBorderWidthAll(1);
        focusedEdit.BorderColor = Palette.UiAccent;
        theme.SetStylebox("normal", "LineEdit", Box(Palette.UiEditor, 6));
        theme.SetStylebox("focus", "LineEdit", focusedEdit);
        theme.SetColor("font_color", "LineEdit", Palette.UiText);

        theme.SetStylebox("normal", "CodeEdit", EditBox());
        theme.SetStylebox("normal", "TextEdit", EditBox());

        theme.SetStylebox("scroll", "VScrollBar", ScrollBox(Palette.UiEditor, 4));
        theme.SetStylebox("grabber", "VScrollBar", ScrollBox(Palette.UiButtonHover, 4));
        theme.SetStylebox("grabber_highlight", "VScrollBar", ScrollBox(Palette.UiAccent, 4));
        theme.SetStylebox("grabber_pressed", "VScrollBar", ScrollBox(Palette.UiAccent, 4));
        theme.SetStylebox("scroll", "HScrollBar", ScrollBox(Palette.UiEditor, 4));
        theme.SetStylebox("grabber", "HScrollBar", ScrollBox(Palette.UiButtonHover, 4));
        theme.SetStylebox("grabber_highlight", "HScrollBar", ScrollBox(Palette.UiAccent, 4));
        theme.SetStylebox("grabber_pressed", "HScrollBar", ScrollBox(Palette.UiAccent, 4));

        theme.SetStylebox("panel", "TooltipPanel", Box(Palette.UiPanel, 6));
        theme.SetColor("font_color", "TooltipLabel", Palette.UiText);
        theme.SetColor("font_color", "Label", Palette.UiText);
        return theme;
    }

    // Tabs are joined to the content below them, so only their top corners are round.
    private static StyleBoxFlat TabBox(Color color)
    {
        StyleBoxFlat box = Box(color, 8);
        box.CornerRadiusBottomLeft = 0;
        box.CornerRadiusBottomRight = 0;
        return box;
    }

    // The text inside the editor needs more room than a button, so its margins are wider.
    private static StyleBoxFlat EditBox()
    {
        StyleBoxFlat box = Box(Palette.UiEditor, 6);
        box.ContentMarginLeft = 8;
        box.ContentMarginRight = 8;
        box.ContentMarginTop = 8;
        box.ContentMarginBottom = 8;
        return box;
    }

    // A scroll bar part has no padding, so its box fills the whole track.
    private static StyleBoxFlat ScrollBox(Color color, int cornerRadius)
    {
        StyleBoxFlat box = Box(color, cornerRadius);
        box.ContentMarginLeft = 0;
        box.ContentMarginRight = 0;
        box.ContentMarginTop = 0;
        box.ContentMarginBottom = 0;
        return box;
    }
}
