using CodingGame.Core.Rules;
using Godot;

namespace CodingGame.Godot;

/// <summary>How far a crop has grown. It picks the crop's shape.</summary>
internal enum GrowthStage { Sprout, Young, Ripe }

/// <summary>The look of a crop, on the field and on its item pill: three leaves, a bulb under leaves, or a bush over tubers.</summary>
internal enum CropShape { Leaves, Bulb, Tubers }

public partial class FieldView3D
{
    private const float SwayAmplitudeRadians = 4f * Mathf.Pi / 180f;
    private const float SwayPeriodSeconds = 1.5f;
    private const float PopInStartScale = 0.6f;
    private const float PopInSeconds = 0.25f;

    /// <summary>Gets the shape a crop is drawn with. Crops take the shapes in turn, in the order of the game rules.</summary>
    /// <param name="crop">The crop. It must be one of <paramref name="crops"/>.</param>
    /// <param name="crops">The crops of the game rules.</param>
    /// <returns>The crop's shape.</returns>
    internal static CropShape ShapeOf(Crop crop, IReadOnlyList<Crop> crops)
    {
        return (CropShape)(CropIndex(crop, crops) % 3);
    }

    private static GrowthStage StageOf(int ageTicks, int growTimeTicks)
    {
        if (ageTicks >= growTimeTicks)
        {
            return GrowthStage.Ripe;
        }

        return 3 * ageTicks < growTimeTicks ? GrowthStage.Sprout : GrowthStage.Young;
    }

    // Within a stage the plant grows from 70% to full size, so growth stays visible.
    private static float GrowthScale(GrowthStage stage, int ageTicks, int growTimeTicks)
    {
        return stage switch
        {
            GrowthStage.Sprout => 0.7f + 0.3f * (3f * ageTicks / growTimeTicks),
            GrowthStage.Young => 0.7f + 0.3f * (1.5f * ((float)ageTicks / growTimeTicks - 1f / 3f)),
            _ => 1f,
        };
    }

    private static SphereMesh LowPolySphere(float radius, float height)
    {
        return new SphereMesh { Radius = radius, Height = height, RadialSegments = 6, Rings = 3 };
    }

    private Node3D BuildCropShape(Crop crop, GrowthStage stage, IReadOnlyList<Crop> crops)
    {
        Color color = Palette.CropColor(crop, crops);
        bool glow = stage == GrowthStage.Ripe;
        Node3D shape = new();
        if (stage == GrowthStage.Sprout)
        {
            shape.AddChild(CropModel("crops_leafsStageA", 0.5f, _ => MaterialFor(Palette.SproutLeaf, false)));
            return shape;
        }

        switch (ShapeOf(crop, crops))
        {
            case CropShape.Leaves:
                SphereMesh cloverLeafMesh = stage == GrowthStage.Ripe ? LowPolySphere(0.13f, 0.05f) : LowPolySphere(0.09f, 0.04f);
                float cloverLeafDistance = stage == GrowthStage.Ripe ? 0.12f : 0.08f;
                foreach (float angleDegrees in new[] { 90f, 210f, 330f })
                {
                    float angle = Mathf.DegToRad(angleDegrees);
                    shape.AddChild(Part(cloverLeafMesh, color, glow, new Vector3(Mathf.Cos(angle) * cloverLeafDistance, 0.04f, Mathf.Sin(angle) * cloverLeafDistance)));
                }

                if (stage == GrowthStage.Ripe)
                {
                    shape.AddChild(Part(new CylinderMesh { TopRadius = 0.008f, BottomRadius = 0.01f, Height = 0.1f, RadialSegments = 4, Rings = 1 }, Palette.Leaf, false, new Vector3(0, 0.05f, 0)));
                    shape.AddChild(Part(LowPolySphere(0.05f, 0.1f), Palette.CloverFlower, false, new Vector3(0, 0.12f, 0)));
                }

                break;
            case CropShape.Bulb:
                // The turnip model's root is the bulb; it takes the crop's colour.
                shape.AddChild(stage == GrowthStage.Ripe
                    ? CropModel("crop_turnip", 0.65f, material => material == "grass" ? MaterialFor(Palette.Leaf, false) : MaterialFor(color, glow))
                    : CropModel("crops_leafsStageB", 0.45f, _ => MaterialFor(Palette.Leaf, false)));
                break;
            case CropShape.Tubers:
                if (stage == GrowthStage.Ripe)
                {
                    shape.AddChild(CropModel("plant_bushDetailed", 1f, _ => MaterialFor(Palette.Leaf, false)));
                    SphereMesh flowerMesh = LowPolySphere(0.04f, 0.08f);
                    foreach (Vector3 position in new[] { new Vector3(0.10f, 0.30f, 0.06f), new Vector3(-0.12f, 0.28f, -0.06f), new Vector3(0, 0.32f, -0.12f) })
                    {
                        shape.AddChild(Part(flowerMesh, Palette.PotatoFlower, false, position));
                    }

                    SphereMesh potatoMesh = LowPolySphere(0.07f, 0.14f);
                    foreach (Vector3 position in new[] { new Vector3(0.26f, 0.05f, 0), new Vector3(-0.13f, 0.05f, 0.22f), new Vector3(-0.13f, 0.05f, -0.22f) })
                    {
                        shape.AddChild(Part(potatoMesh, color, glow, position));
                    }
                }
                else
                {
                    shape.AddChild(CropModel("plant_bush", 0.9f, _ => MaterialFor(Palette.Leaf, false)));
                }

                break;
        }

        return shape;
    }

    private void SwayRipeCrops()
    {
        foreach (TileView view in _tileViews)
        {
            if (view.ShownCrop is { Stage: GrowthStage.Ripe })
            {
                float angle = _elapsedSeconds * Mathf.Tau / SwayPeriodSeconds + view.SwayPhaseRadians;
                view.CropSlot.Rotation = new Vector3(SwayAmplitudeRadians * Mathf.Sin(angle), 0, SwayAmplitudeRadians * Mathf.Sin(angle + 1.3f));
            }
        }
    }
}
