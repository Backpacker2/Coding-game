using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;
using Godot;

namespace CodingGame.Godot;

public partial class FieldView3D
{
    private void ShowFeedback(Field field, IReadOnlyList<Crop> crops)
    {
        // Harvests are found by comparing two frames, so at Fast speed several harvests within one frame show as one
        // burst per tile and one summed text per item. That is fine for feedback.
        for (int x = 0; x < field.Width; x++)
        {
            for (int y = 0; y < field.Height; y++)
            {
                ShowTileFeedback(_tileViews[x, y], field.GetTile(x, y), new Vector3(x, 0.2f, y), crops);
            }
        }

        ShowItemGains(field);
    }

    private void ShowTileFeedback(TileView view, Tile tile, Vector3 position, IReadOnlyList<Crop> crops)
    {
        if (view.ShownCrop is { Stage: GrowthStage.Ripe } harvested && (tile.Crop is null || tile.CropAgeTicks == 0))
        {
            // The player had none of this crop's item, so this is the first harvest of a new crop (or a fresh start):
            // it gets a bigger burst. Read from the shown items, so nothing extra is saved.
            bool first = _shownItems.GetValueOrDefault(harvested.Crop.Name) == 0;
            SpawnBurst(position, Palette.CropColor(harvested.Crop, crops), first ? 48 : 16, 1.5f, first ? 4f : 2.5f);
        }
        else if (view.ShownCrop is null && tile.Crop is not null)
        {
            SpawnBurst(position, Palette.PlantingSoil, 8, 0.8f, 1.2f);
        }
    }

    private void ShowItemGains(Field field)
    {
        Vector3 textStart = new(field.DronePosition.X, 1.3f, field.DronePosition.Y);
        foreach ((string item, int count) in field.Items)
        {
            int gained = count - _shownItems.GetValueOrDefault(item);
            if (gained > 0)
            {
                SpawnFloatingText(textStart, $"+{gained} {item}");
            }
        }
    }

    private void SpawnBurst(Vector3 position, Color color, int amount, float minSpeed, float maxSpeed)
    {
        CpuParticles3D burst = new()
        {
            OneShot = true,
            Emitting = true,
            Amount = amount,
            Lifetime = 0.6f,
            Explosiveness = 1f,
            Direction = Vector3.Up,
            Spread = 60f,
            InitialVelocityMin = minSpeed,
            InitialVelocityMax = maxSpeed,
            Gravity = new Vector3(0, -6, 0),
            ScaleAmountMin = 0.6f,
            ScaleAmountMax = 1f,
            Mesh = new SphereMesh { Radius = 0.04f, Height = 0.08f, Material = MaterialFor(color, false) },
            Position = position,
        };
        _effects.AddChild(burst);
        burst.Finished += burst.QueueFree;
    }

    private void SpawnFloatingText(Vector3 start, string text)
    {
        Label3D label = new()
        {
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Text = text,
            FontSize = 48,
            OutlineSize = 8,
            NoDepthTest = true,
            PixelSize = 0.004f,
            Position = start,
        };
        _effects.AddChild(label);
        Tween tween = label.CreateTween();
        tween.TweenProperty(label, "position:y", start.Y + 0.6f, 1f);
        tween.Parallel().TweenProperty(label, "modulate:a", 0f, 1f);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }
}
