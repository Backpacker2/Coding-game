using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;
using Godot;
using Environment = Godot.Environment;

namespace CodingGame.Godot;

/// <summary>
/// Shows the farm field as a low-poly 3D scene seen from an angle: grass and soil tiles, the crops that grow on them
/// and the drone hovering over the field. It only reads the field. The drone's hover and rotors move in real time.
/// </summary>
public partial class FieldView3D : SubViewportContainer
{
    // Placeholder art: the colours only tell the crops apart until real art exists.
    private static readonly Color[] _cropColors = [Colors.Gold, Colors.Red, Colors.Orange];
    private static readonly Color _grassColor = new(0.3f, 0.52f, 0.22f);
    private static readonly Color _soilColor = new(0.55f, 0.36f, 0.2f);
    private static readonly Color _baseColor = new(0.4f, 0.26f, 0.15f);
    private static readonly Color _leafColor = new(0.25f, 0.55f, 0.2f);
    private static readonly Color _droneBodyColor = new(0.85f, 0.87f, 0.9f);
    private static readonly Color _droneEyeColor = new(0.3f, 0.6f, 1f);
    private static readonly Color _rotorColor = new(0.2f, 0.2f, 0.22f);
    private const float RotorSpeedRadiansPerSecond = 20f;
    private const float GlideRatePerSecond = 12f;
    private const float MaxGlideDistanceTiles = 1.5f;
    private const float LeanRadiansPerTilePerSecond = 0.15f;
    private const float MaxLeanRadians = 0.35f;

    private sealed class TileView(MeshInstance3D tileMesh, Node3D cropSlot)
    {
        public MeshInstance3D TileMesh { get; } = tileMesh;

        public Node3D CropSlot { get; } = cropSlot;

        public Ground? ShownGround { get; set; }

        public ShownCrop? ShownCrop { get; set; }
    }

    private readonly record struct ShownCrop(Crop Crop, bool Ripe);

    private readonly BoxMesh _tileMesh = new() { Size = new Vector3(0.94f, 0.3f, 0.94f) };
    private readonly Dictionary<(Color Color, bool Glow), StandardMaterial3D> _materials = [];
    private readonly List<Node3D> _rotors = [];
    private Node3D _tiles = null!;
    private Node3D _drone = null!;
    private Camera3D _camera = null!;
    private TileView[,] _tileViews = new TileView[0, 0];
    private float _elapsedSeconds;
    private Node3D _effects = null!;
    private Field? _shownField;
    private Dictionary<string, int> _shownItems = [];
    private Vector2 _droneTarget;
    private Vector2 _droneShown;

    /// <summary>
    /// Creates the view. Its 3D scene is built when Godot readies the view.
    /// </summary>
    public FieldView3D()
    {
        Stretch = true;
    }

    /// <summary>
    /// Gets the colour that draws a crop on the field and in the item bar.
    /// </summary>
    /// <param name="crop">The crop to colour. It must be one of <paramref name="crops"/>.</param>
    /// <param name="crops">The crops of the game rules. A crop's index here picks its colour.</param>
    /// <returns>The colour of the crop.</returns>
    public static Color CropColor(Crop crop, IReadOnlyList<Crop> crops)
    {
        return _cropColors[CropIndex(crop, crops) % _cropColors.Length];
    }

    /// <summary>
    /// Shows the field and the crops on it, and moves the drone to its tile. Call it every frame.
    /// The tiles are rebuilt only when the size of the field changes. A harvest or a planting since the last
    /// call shows a burst, and the items gained show as text. A new field, or a drone that jumps across the field
    /// because it wrapped at an edge, is shown at once. It only reads the field.
    /// </summary>
    /// <param name="field">The field to show. It is only read, never changed.</param>
    /// <param name="crops">The crops of the game rules. A crop's index here picks its colour and its shape.</param>
    public void ShowField(Field field, IReadOnlyList<Crop> crops)
    {
        bool newField = !ReferenceEquals(field, _shownField);
        _shownField = field;
        if (_tileViews.GetLength(0) != field.Width || _tileViews.GetLength(1) != field.Height)
        {
            BuildTiles(field.Width, field.Height);
        }

        if (!newField)
        {
            ShowFeedback(field, crops);
        }

        for (int x = 0; x < field.Width; x++)
        {
            for (int y = 0; y < field.Height; y++)
            {
                ShowTile(_tileViews[x, y], field.GetTile(x, y), crops);
            }
        }

        _droneTarget = new Vector2(field.DronePosition.X, field.DronePosition.Y);
        Vector2 offset = _droneTarget - _droneShown;
        if (newField || Mathf.Abs(offset.X) > MaxGlideDistanceTiles || Mathf.Abs(offset.Y) > MaxGlideDistanceTiles)
        {
            _droneShown = _droneTarget;
        }

        _shownItems = new Dictionary<string, int>(field.Items);
    }

    /// <inheritdoc/>
    public override void _Ready()
    {
        SubViewport viewport = new() { OwnWorld3D = true, TransparentBg = true, Msaa3D = Viewport.Msaa.Msaa4X };
        AddChild(viewport);

        _tiles = new Node3D();
        _effects = new Node3D();
        _drone = BuildDrone();
        _camera = new Camera3D { Fov = 40 };
        // The compatibility renderer has no tone mapping by default, so a full-strength sun washes the grass out.
        DirectionalLight3D light = new()
        {
            ShadowEnabled = true,
            RotationDegrees = new Vector3(-55, -35, 0),
            LightEnergy = 0.75f,
        };
        WorldEnvironment worldEnvironment = new()
        {
            Environment = new Environment
            {
                AmbientLightSource = Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.75f, 0.8f, 0.9f),
                AmbientLightEnergy = 0.4f,
            },
        };
        viewport.AddChild(_tiles);
        viewport.AddChild(_effects);
        viewport.AddChild(_drone);
        viewport.AddChild(_camera);
        viewport.AddChild(light);
        viewport.AddChild(worldEnvironment);
    }

    /// <inheritdoc/>
    public override void _Process(double delta)
    {
        _elapsedSeconds += (float)delta;
        UpdateDrone(delta);
        foreach (Node3D rotor in _rotors)
        {
            rotor.RotateY(RotorSpeedRadiansPerSecond * (float)delta);
        }
    }

    private void UpdateDrone(double delta)
    {
        Vector2 previous = _droneShown;
        _droneShown = _droneShown.Lerp(_droneTarget, 1 - Mathf.Exp(-GlideRatePerSecond * (float)delta));
        Vector2 velocity = (_droneShown - previous) / (float)delta;
        Vector2 lean = velocity.Normalized() * Mathf.Min(velocity.Length() * LeanRadiansPerTilePerSecond, MaxLeanRadians);
        float hover = 0.9f + 0.05f * Mathf.Sin(_elapsedSeconds * 2);
        _drone.Position = new Vector3(_droneShown.X, hover, _droneShown.Y);
        _drone.Rotation = new Vector3(lean.Y, 0, -lean.X);
    }

    private static int CropIndex(Crop crop, IReadOnlyList<Crop> crops)
    {
        return crops.Index().First(entry => entry.Item == crop).Index;
    }

    private static void FreeChildren(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            node.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void BuildTiles(int width, int height)
    {
        FreeChildren(_tiles);
        _tileViews = new TileView[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                MeshInstance3D tileMesh = new() { Mesh = _tileMesh, Position = new Vector3(x, -0.15f, y) };
                Node3D cropSlot = new() { Position = new Vector3(x, 0, y), Visible = false };
                _tiles.AddChild(tileMesh);
                _tiles.AddChild(cropSlot);
                _tileViews[x, y] = new TileView(tileMesh, cropSlot);
            }
        }

        _tiles.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(width + 0.2f, 0.2f, height + 0.2f) },
            MaterialOverride = MaterialFor(_baseColor, false),
            Position = new Vector3((width - 1) / 2f, -0.4f, (height - 1) / 2f),
        });

        Vector3 target = new((width - 1) / 2f, 0, (height - 1) / 2f);
        Vector3 direction = new Vector3(0, 1, 1).Normalized().Rotated(Vector3.Up, Mathf.DegToRad(35));
        float distance = Mathf.Max(width, height) * 1.8f + 2;
        _camera.LookAtFromPosition(target + direction * distance, target, Vector3.Up);
    }

    private void ShowTile(TileView view, Tile tile, IReadOnlyList<Crop> crops)
    {
        if (view.ShownGround != tile.Ground)
        {
            view.TileMesh.MaterialOverride = MaterialFor(tile.Ground == Ground.Grass ? _grassColor : _soilColor, false);
            view.ShownGround = tile.Ground;
        }

        if (tile.Crop is null)
        {
            view.CropSlot.Visible = false;
            view.ShownCrop = null;
            return;
        }

        bool ripe = tile.CropAgeTicks >= tile.Crop.GrowTimeTicks;
        ShownCrop shown = new(tile.Crop, ripe);
        if (view.ShownCrop != shown)
        {
            FreeChildren(view.CropSlot);
            view.CropSlot.AddChild(BuildCropShape(tile.Crop, ripe, crops));
            view.ShownCrop = shown;
        }

        float growth = Mathf.Min(1f, (float)tile.CropAgeTicks / tile.Crop.GrowTimeTicks);
        view.CropSlot.Visible = true;
        view.CropSlot.Scale = Vector3.One * (0.3f + 0.7f * growth);
    }

    private StandardMaterial3D MaterialFor(Color color, bool glow)
    {
        if (!_materials.TryGetValue((color, glow), out StandardMaterial3D? material))
        {
            material = new StandardMaterial3D { AlbedoColor = color };
            if (glow)
            {
                material.EmissionEnabled = true;
                material.Emission = color;
                material.EmissionEnergyMultiplier = 0.4f;
            }

            _materials[(color, glow)] = material;
        }

        return material;
    }

    private MeshInstance3D Part(Mesh mesh, Color color, bool glow, Vector3 position)
    {
        return new MeshInstance3D { Mesh = mesh, MaterialOverride = MaterialFor(color, glow), Position = position };
    }

    private Node3D BuildCropShape(Crop crop, bool ripe, IReadOnlyList<Crop> crops)
    {
        Color color = CropColor(crop, crops);
        Node3D shape = new();
        switch (CropIndex(crop, crops) % 3)
        {
            case 0:
                SphereMesh berry = new() { Radius = 0.14f, Height = 0.12f };
                foreach (Vector3 position in new[] { new Vector3(0, 0.06f, -0.15f), new Vector3(0.13f, 0.06f, 0.075f), new Vector3(-0.13f, 0.06f, 0.075f) })
                {
                    shape.AddChild(Part(berry, color, ripe, position));
                }

                break;
            case 1:
                shape.AddChild(Part(new SphereMesh { Radius = 0.18f, Height = 0.36f }, color, ripe, new Vector3(0, 0.12f, 0)));
                PrismMesh leaf = new() { Size = new Vector3(0.08f, 0.2f, 0.02f) };
                MeshInstance3D leftLeaf = Part(leaf, _leafColor, false, new Vector3(-0.08f, 0.34f, 0));
                leftLeaf.RotationDegrees = new Vector3(0, 0, 30);
                MeshInstance3D rightLeaf = Part(leaf, _leafColor, false, new Vector3(0.08f, 0.34f, 0));
                rightLeaf.RotationDegrees = new Vector3(0, 0, -30);
                shape.AddChild(leftLeaf);
                shape.AddChild(rightLeaf);
                break;
            default:
                shape.AddChild(Part(new SphereMesh { Radius = 0.28f, Height = 0.36f }, _leafColor, false, new Vector3(0, 0.18f, 0)));
                SphereMesh fruit = new() { Radius = 0.07f, Height = 0.14f };
                foreach (Vector3 position in new[] { new Vector3(0.2f, 0.07f, 0), new Vector3(-0.1f, 0.07f, 0.17f), new Vector3(-0.1f, 0.07f, -0.17f) })
                {
                    shape.AddChild(Part(fruit, color, ripe, position));
                }

                break;
        }

        return shape;
    }

    private Node3D BuildDrone()
    {
        Node3D drone = new();
        drone.AddChild(Part(new CylinderMesh { TopRadius = 0.18f, BottomRadius = 0.18f, Height = 0.12f }, _droneBodyColor, false, Vector3.Zero));
        drone.AddChild(Part(new SphereMesh { Radius = 0.06f, Height = 0.12f }, _droneEyeColor, true, new Vector3(0, 0.02f, -0.18f)));
        foreach (float armAngle in new[] { 45f, -45f })
        {
            MeshInstance3D arm = Part(new BoxMesh { Size = new Vector3(0.5f, 0.03f, 0.04f) }, _droneBodyColor, false, new Vector3(0, 0.03f, 0));
            arm.RotationDegrees = new Vector3(0, armAngle, 0);
            drone.AddChild(arm);
        }

        CylinderMesh rotorMesh = new() { TopRadius = 0.12f, BottomRadius = 0.12f, Height = 0.01f };
        foreach (Vector3 position in new[] { new Vector3(0.18f, 0.06f, 0.18f), new Vector3(-0.18f, 0.06f, 0.18f), new Vector3(0.18f, 0.06f, -0.18f), new Vector3(-0.18f, 0.06f, -0.18f) })
        {
            MeshInstance3D rotor = Part(rotorMesh, _rotorColor, false, position);
            drone.AddChild(rotor);
            _rotors.Add(rotor);
        }

        return drone;
    }
}
