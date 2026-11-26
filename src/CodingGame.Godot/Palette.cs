using CodingGame.Core.Rules;
using Godot;

namespace CodingGame.Godot;

/// <summary>
/// The colours of the field, the drone, the sky, the script editor and the code window, in one place.
/// </summary>
public static class Palette
{
    /// <summary>Grass tile.</summary>
    public static readonly Color Grass = new(0.33f, 0.50f, 0.24f);

    /// <summary>Soil tile.</summary>
    public static readonly Color Soil = new(0.45f, 0.33f, 0.24f);

    /// <summary>Grass of the island.</summary>
    public static readonly Color Ground = new(0.50f, 0.60f, 0.34f);

    /// <summary>Soil and rock under the island.</summary>
    public static readonly Color IslandEdge = new(0.36f, 0.27f, 0.20f);

    /// <summary>Sides of a tile under its top layer.</summary>
    public static readonly Color TileBody = new(0.40f, 0.29f, 0.21f);

    /// <summary>Furrows on tilled soil.</summary>
    public static readonly Color Furrow = new(0.30f, 0.21f, 0.14f);

    /// <summary>Trunks of the trees.</summary>
    public static readonly Color TreeTrunk = new(0.35f, 0.25f, 0.17f);

    /// <summary>Crowns of the trees.</summary>
    public static readonly Color TreeCrown = new(0.22f, 0.42f, 0.22f);

    /// <summary>Rocks on the island.</summary>
    public static readonly Color Rock = new(0.52f, 0.52f, 0.50f);

    /// <summary>Highlight under the drone.</summary>
    public static readonly Color DroneRing = new(0.30f, 0.75f, 1.00f, 0.35f);

    /// <summary>Soil that has just been planted.</summary>
    public static readonly Color PlantingSoil = new(0.40f, 0.29f, 0.20f);

    /// <summary>Leaves of the crops.</summary>
    public static readonly Color Leaf = new(0.30f, 0.50f, 0.24f);

    /// <summary>Leaves of a sprout, lighter than a grown plant.</summary>
    public static readonly Color SproutLeaf = new(0.45f, 0.65f, 0.30f);

    /// <summary>Flower ball of a ripe clover.</summary>
    public static readonly Color CloverFlower = new(0.98f, 0.85f, 0.88f);

    /// <summary>Flowers of a ripe potato bush.</summary>
    public static readonly Color PotatoFlower = new(0.97f, 0.96f, 0.92f);

    /// <summary>Body and arms of the drone.</summary>
    public static readonly Color DroneBody = new(0.82f, 0.84f, 0.86f);

    /// <summary>The light on the front of the drone.</summary>
    public static readonly Color DroneLight = new(0.30f, 0.75f, 1.00f);

    /// <summary>Rotors of the drone.</summary>
    public static readonly Color Rotor = new(0.20f, 0.21f, 0.23f);

    /// <summary>Top of the sky.</summary>
    public static readonly Color SkyTop = new(0.35f, 0.55f, 0.80f);

    /// <summary>Sky at the horizon.</summary>
    public static readonly Color SkyHorizon = new(0.78f, 0.82f, 0.85f);

    /// <summary>Ground at the horizon.</summary>
    public static readonly Color SkyGroundHorizon = new(0.70f, 0.78f, 0.84f);

    /// <summary>Ground below the horizon.</summary>
    public static readonly Color SkyGroundBottom = new(0.50f, 0.62f, 0.75f);

    /// <summary>Fog that fades the far distance.</summary>
    public static readonly Color Fog = new(0.78f, 0.82f, 0.85f);

    /// <summary>Colour of the sun.</summary>
    public static readonly Color Sun = new(1f, 0.88f, 0.70f);

    /// <summary>Colour of the ambient light.</summary>
    public static readonly Color Ambient = new(0.62f, 0.70f, 0.85f);

    /// <summary>Keywords of the script languages. Bright enough to read on the dark editor background.</summary>
    public static readonly Color SyntaxKeyword = new(0.8f, 0.47f, 0.87f);

    /// <summary>Drone commands and function calls.</summary>
    public static readonly Color SyntaxCommand = new(0.4f, 0.73f, 1f);

    /// <summary>Drone constants.</summary>
    public static readonly Color SyntaxConstant = new(0.95f, 0.75f, 0.35f);

    /// <summary>Number literals.</summary>
    public static readonly Color SyntaxNumber = new(0.6f, 0.85f, 0.55f);

    /// <summary>String literals.</summary>
    public static readonly Color SyntaxString = new(0.85f, 0.6f, 0.45f);

    /// <summary>Comments.</summary>
    public static readonly Color SyntaxComment = new(0.55f, 0.58f, 0.62f);

    /// <summary>Brackets and operators. The highlighter's default black would hide them on the dark background.</summary>
    public static readonly Color SyntaxSymbol = new(0.85f, 0.85f, 0.85f);

    /// <summary>The hint in the messages box while there is no error.</summary>
    public static readonly Color MutedText = new(0.6f, 0.6f, 0.6f);

    /// <summary>Text of an error in the messages box.</summary>
    public static readonly Color ErrorText = Colors.Red;

    /// <summary>Text of a hint under the messages box: calm, so it reads as help and not as another error.</summary>
    public static readonly Color HintText = new(0.55f, 0.78f, 0.90f);

    /// <summary>Background of the line where a run failed.</summary>
    public static readonly Color ErrorLineHighlight = new(1f, 0f, 0f, 0.35f);

    /// <summary>Background of the line the run is on.</summary>
    public static readonly Color RunningLineHighlight = new(1f, 1f, 0f, 0.25f);

    /// <summary>Background of the goal bar under the item bar.</summary>
    public static readonly Color GoalBarBackground = new(0f, 0f, 0f, 0.25f);

    /// <summary>Background of the banner for a bought unlock, dark so its text reads over the bright field.</summary>
    public static readonly Color BannerBackground = new(0.10f, 0.13f, 0.10f, 0.85f);

    /// <summary>Pulse on the Unlocks button when an unlock can be bought.</summary>
    public static readonly Color UnlockGlow = new(1.0f, 0.92f, 0.55f);

    /// <summary>Text of a first-steps goal that was just done.</summary>
    public static readonly Color GoalDone = new(0.60f, 0.88f, 0.50f);

    /// <summary>Background of the first-steps card under the goal bar.</summary>
    public static readonly Color GoalCardBackground = new(0f, 0f, 0f, 0.35f);

    /// <summary>Background of windows, dialogs, menus and panels.</summary>
    public static readonly Color UiPanel = new(0.17f, 0.16f, 0.14f, 0.94f);

    /// <summary>Thin border and title bar line around windows.</summary>
    public static readonly Color UiPanelBorder = new(0.36f, 0.31f, 0.24f);

    /// <summary>Normal text on the UI panels.</summary>
    public static readonly Color UiText = new(0.94f, 0.91f, 0.84f);

    /// <summary>A button at rest, and an unselected tab.</summary>
    public static readonly Color UiButton = new(0.29f, 0.27f, 0.23f);

    /// <summary>A button under the mouse, and the selected tab.</summary>
    public static readonly Color UiButtonHover = new(0.37f, 0.34f, 0.29f);

    /// <summary>A button while it is pressed.</summary>
    public static readonly Color UiButtonPressed = new(0.22f, 0.20f, 0.17f);

    /// <summary>Main action colour: Run and the round buttons at the top right.</summary>
    public static readonly Color UiAccent = new(0.33f, 0.58f, 0.28f);

    /// <summary>Stop, the button that ends a run.</summary>
    public static readonly Color UiDanger = new(0.68f, 0.27f, 0.23f);

    /// <summary>Background of the code editor, the Messages box and text fields.</summary>
    public static readonly Color UiEditor = new(0.11f, 0.11f, 0.12f);

    // Placeholder art: the colours only tell the crops apart until real art exists.
    // The order follows the crops in data/crops.json: clover, radish, potato.
    private static readonly Color[] _cropColors = [new(0.14f, 0.40f, 0.20f), new(0.85f, 0.28f, 0.35f), new(0.82f, 0.66f, 0.38f)];

    /// <summary>
    /// Gets the colour that draws a crop on the field and in the item bar.
    /// </summary>
    /// <param name="crop">The crop to colour. It must be one of <paramref name="crops"/>.</param>
    /// <param name="crops">The crops of the game rules. A crop's index here picks its colour.</param>
    /// <returns>The colour of the crop.</returns>
    public static Color CropColor(Crop crop, IReadOnlyList<Crop> crops)
    {
        return _cropColors[FieldView3D.CropIndex(crop, crops) % _cropColors.Length];
    }

    /// <summary>
    /// Gets a colour a little lighter or darker, so neighbouring tiles differ slightly.
    /// </summary>
    /// <param name="color">The colour to shade.</param>
    /// <param name="shade">0 for darker, 1 for the colour itself, 2 for lighter.</param>
    /// <returns>The shaded colour.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="shade"/> is not 0, 1 or 2.</exception>
    public static Color Shaded(Color color, int shade)
    {
        return shade switch
        {
            0 => color.Darkened(0.05f),
            1 => color,
            2 => color.Lightened(0.05f),
            _ => throw new ArgumentOutOfRangeException(nameof(shade), shade, "shade must be 0, 1 or 2"),
        };
    }
}
