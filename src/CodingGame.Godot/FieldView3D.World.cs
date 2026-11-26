using Godot;

namespace CodingGame.Godot;

public partial class FieldView3D
{
    // The island reaches this many tiles past the field on every side.
    private const int IslandMarginTiles = 3;

    // Trees and rocks stand on the island ground, which is this high.
    private const float GroundTopY = -0.22f;

    // Decoration and tile shades come from the position, so the view looks the same every time without randomness.
    private static uint CellHash(int x, int y)
    {
        unchecked
        {
            uint hash = ((uint)x * 73856093u) ^ ((uint)y * 19349663u);
            hash ^= hash >> 13;
            hash *= 0x85EBCA6Bu;
            hash ^= hash >> 16;
            return hash;
        }
    }

    private void BuildWorld(int width, int height)
    {
        float cx = (width - 1) / 2f;
        float cz = (height - 1) / 2f;
        _tiles.AddChild(Part(new BoxMesh { Size = new Vector3(width + 2 * IslandMarginTiles, 0.3f, height + 2 * IslandMarginTiles) }, Palette.Ground, false, new Vector3(cx, GroundTopY - 0.15f, cz)));
        // Narrower than the ground, so the island tapers.
        _tiles.AddChild(Part(new BoxMesh { Size = new Vector3(width + 2 * IslandMarginTiles - 0.6f, 0.6f, height + 2 * IslandMarginTiles - 0.6f) }, Palette.IslandEdge, false, new Vector3(cx, GroundTopY - 0.6f, cz)));

        for (int x = -IslandMarginTiles; x < width + IslandMarginTiles; x++)
        {
            for (int z = -IslandMarginTiles; z < height + IslandMarginTiles; z++)
            {
                // Keeps tree crowns off the field and the drone's path.
                if (x >= -1 && x <= width && z >= -1 && z <= height)
                {
                    continue;
                }

                uint hash = CellHash(x, z);
                if (hash % 3 != 0)
                {
                    continue;
                }

                bool rock = (hash >> 4) % 3 == 0;
                float offsetX = (((hash >> 8) & 0xFF) / 255f - 0.5f) * 0.6f;
                float offsetZ = (((hash >> 16) & 0xFF) / 255f - 0.5f) * 0.6f;
                float size = 0.8f + ((hash >> 24) & 0xFF) / 255f * 0.5f;
                Node3D item = new()
                {
                    Position = new Vector3(x + offsetX, GroundTopY, z + offsetZ),
                    Scale = Vector3.One * size,
                    RotationDegrees = new Vector3(0, hash % 360, 0),
                };
                item.AddChild(rock ? RockModel(hash >> 2) : TreeModel(hash >> 2));

                _tiles.AddChild(item);
            }
        }
    }

    // Not MaterialFor: that cache is keyed by colour only and creates materials without transparency.
    private static MeshInstance3D BuildDroneRing()
    {
        return new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.45f, BottomRadius = 0.45f, Height = 0.01f, RadialSegments = 32, Rings = 1 },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = Palette.DroneRing,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
    }

    private TileView BuildTileView(int x, int y)
    {
        int shade = (int)(CellHash(x, y) % 3);
        _tiles.AddChild(Part(new BoxMesh { Size = new Vector3(0.94f, 0.24f, 0.94f) }, Palette.Shaded(Palette.TileBody, shade), false, new Vector3(x, -0.16f, y)));

        // The top layer takes its colour from ShowTile, so it has no material here.
        MeshInstance3D top = new() { Mesh = new BoxMesh { Size = new Vector3(0.9f, 0.06f, 0.9f) }, Position = new Vector3(x, -0.03f, y) };
        _tiles.AddChild(top);

        BoxMesh furrowMesh = new() { Size = new Vector3(0.8f, 0.02f, 0.06f) };
        Node3D furrows = new() { Visible = false };
        foreach (float furrowZ in new[] { -0.25f, 0f, 0.25f })
        {
            MeshInstance3D furrow = Part(furrowMesh, Palette.Furrow, false, new Vector3(0, 0.035f, furrowZ));
            furrow.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            furrows.AddChild(furrow);
        }

        top.AddChild(furrows);

        Node3D cropSlot = new() { Position = new Vector3(x, 0, y), Visible = false };
        _tiles.AddChild(cropSlot);
        // The phase comes from the position, so tiles do not sway in sync.
        float swayPhaseRadians = (CellHash(x, y) % 628) / 100f;
        return new TileView(top, cropSlot, furrows, shade, swayPhaseRadians);
    }
}
