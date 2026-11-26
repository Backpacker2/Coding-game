using Godot;

namespace CodingGame.Godot;

/// <summary>
/// The small line icons of the game UI, drawn in white so a button's font colour can tint them.
/// They are made from SVG text at start-up, so no image files need importing.
/// </summary>
public static class GameIcons
{
    // A 20 by 20 canvas with round 2-pixel strokes matches the 16-pixel UI font next to it.
    private const string SvgStart = """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="#ffffff" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">""";

    /// <summary>Angle brackets with a slash, for the button that opens the script window.</summary>
    public static Texture2D Code { get; } = FromSvg("""<path d="M6 5 1.5 10 6 15M14 5l4.5 5-4.5 5M11.5 3.5l-3 13"/>""");

    /// <summary>An open padlock, for the button that opens the unlocks.</summary>
    public static Texture2D Unlocks { get; } = FromSvg("""<rect x="3" y="9" width="12" height="9" rx="2"/><path d="M6 9V6a4 4 0 0 1 7.5-2"/><path d="M9 12.5v2"/>""");

    /// <summary>A question mark in a circle, for the button that opens the help.</summary>
    public static Texture2D Help { get; } = FromSvg("""<circle cx="10" cy="10" r="8"/><path d="M7.5 7.8a2.6 2.6 0 1 1 3.6 2.4c-.7.3-1.1.9-1.1 1.6v.4"/><path d="M10 14.8v.2"/>""");

    /// <summary>Three lines, for the menu button.</summary>
    public static Texture2D Menu { get; } = FromSvg("""<path d="M3 5h14M3 10h14M3 15h14"/>""");

    private static ImageTexture FromSvg(string shapes)
    {
        Image image = new();
        Error error = image.LoadSvgFromString(SvgStart + shapes + "</svg>", 1f);
        if (error != Error.Ok)
        {
            throw new InvalidOperationException($"An icon's SVG text could not be read: {error}.");
        }

        return ImageTexture.CreateFromImage(image);
    }
}
