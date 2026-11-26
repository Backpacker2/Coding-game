using CodingGame.Core.Rules;
using Godot;

namespace CodingGame.Godot;

/// <summary>
/// The colours of the field, the drone, the sky, the script editor and the code window, in one place.
/// </summary>
public static class Palette
{
    /// <summary>Grass tile.</summary>
    public static readonly Color Grass = new(0.38f, 0.54f, 0.27f);

    /// <summary>Soil tile.</summary>
    public static readonly Color Soil = new(0.45f, 0.33f, 0.24f);

    /// <summary>The block under the field.</summary>
    public static readonly Color FieldBase = new(0.33f, 0.24f, 0.17f);

    /// <summary>Soil that has just been planted.</summary>
    public static readonly Color PlantingSoil = new(0.40f, 0.29f, 0.20f);

    /// <summary>Leaves of the crops.</summary>
    public static readonly Color Leaf = new(0.30f, 0.50f, 0.24f);

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

    /// <summary>Background of the line where a run failed.</summary>
    public static readonly Color ErrorLineHighlight = new(1f, 0f, 0f, 0.35f);

    /// <summary>Background of the line the run is on.</summary>
    public static readonly Color RunningLineHighlight = new(1f, 1f, 0f, 0.25f);

    // Placeholder art: the colours only tell the crops apart until real art exists.
    // The order follows the crops in data/crops.json: clover, radish, potato.
    private static readonly Color[] _cropColors = [new(0.18f, 0.55f, 0.25f), new(0.85f, 0.28f, 0.35f), new(0.82f, 0.66f, 0.38f)];

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
}
