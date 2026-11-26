using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;
using Godot;

namespace CodingGame.Godot;

/// <summary>Draws the farm field and the drone. It only reads the field.</summary>
public partial class FarmView : Control
{
    // Placeholder art: the colours only tell the crops apart until real art exists.
    private static readonly Color[] _cropColors = [Colors.Gold, Colors.Red, Colors.Orange];

    private Field? _field;
    private IReadOnlyList<Crop> _crops = [];

    /// <summary>
    /// Sets the field and the crops to draw, then asks Godot to redraw the view.
    /// </summary>
    /// <param name="field">The field to draw. It is only read, never changed.</param>
    /// <param name="crops">The crops of the game rules. A crop's index here picks its colour.</param>
    public void ShowField(Field field, IReadOnlyList<Crop> crops)
    {
        _field = field;
        _crops = crops;
        QueueRedraw();
    }

    /// <summary>
    /// Gets the colour that draws a crop on the farm and in the item bar.
    /// </summary>
    /// <param name="crop">The crop to colour. It must be one of <paramref name="crops"/>.</param>
    /// <param name="crops">The crops of the game rules. A crop's index here picks its colour.</param>
    /// <returns>The colour of the crop.</returns>
    public static Color CropColor(Crop crop, IReadOnlyList<Crop> crops)
    {
        int cropIndex = crops.Index().First(entry => entry.Item == crop).Index;
        return _cropColors[cropIndex % _cropColors.Length];
    }

    /// <inheritdoc/>
    public override void _Draw()
    {
        if (_field is null)
        {
            return;
        }

        Field field = _field;
        float tile = Mathf.Min(Size.X / field.Width, Size.Y / field.Height);
        Vector2 offset = (Size - new Vector2(tile * field.Width, tile * field.Height)) / 2;
        for (int x = 0; x < field.Width; x++)
        {
            for (int y = 0; y < field.Height; y++)
            {
                Tile fieldTile = field.GetTile(x, y);
                Rect2 rect = new(offset.X + x * tile, offset.Y + y * tile, tile - 2, tile - 2);
                DrawRect(rect, fieldTile.Ground == Ground.Grass ? Colors.DarkGreen : Colors.SaddleBrown);
                if (fieldTile.Crop is not null)
                {
                    DrawCrop(rect, fieldTile, tile);
                }
            }
        }

        Vector2 dronePosition = offset + new Vector2(field.DronePosition.X * tile, field.DronePosition.Y * tile);
        DrawRect(new Rect2(dronePosition + new Vector2(4, 4), tile - 8, tile - 8), Colors.CornflowerBlue, filled: false, width: 3);
        Vector2 droneCenter = dronePosition + new Vector2(tile * 0.25f, tile * 0.25f);
        DrawCircle(droneCenter, tile * 0.18f, Colors.CornflowerBlue);
        DrawArc(droneCenter, tile * 0.18f, 0, Mathf.Tau, 32, Colors.White, 2);
    }

    private void DrawCrop(Rect2 rect, Tile fieldTile, float tile)
    {
        Crop crop = fieldTile.Crop!;
        float growth = Mathf.Min(1f, (float)fieldTile.CropAgeTicks / crop.GrowTimeTicks);
        float radius = tile / 4 * (0.3f + 0.7f * growth);
        Color color = CropColor(crop, _crops);
        Vector2 center = rect.GetCenter();
        DrawCircle(center, radius, color);
        if (fieldTile.CropAgeTicks >= crop.GrowTimeTicks)
        {
            DrawArc(center, radius, 0, Mathf.Tau, 32, Colors.White, 2);
        }
    }
}
