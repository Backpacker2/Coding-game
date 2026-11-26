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
    private const float RotorSpeedRadiansPerSecond = 20f;
    private const float GlideRatePerSecond = 12f;
    private const float MaxGlideDistanceTiles = 1.5f;
    private const float LeanRadiansPerTilePerSecond = 0.15f;
    private const float MaxLeanRadians = 0.35f;

    // The script window starts at 55% of the screen width (Main), so the field is centred in the left 55%.
    private const float FieldCentreScreenFraction = 0.275f;

    // Leaves room for the item bar at the top.
    private const float FieldLowerFraction = 0.04f;

    private sealed class TileView(MeshInstance3D tileMesh, Node3D cropSlot, Node3D furrows, int shade)
    {
        public MeshInstance3D TileMesh { get; } = tileMesh;

        public Node3D CropSlot { get; } = cropSlot;

        public Node3D Furrows { get; } = furrows;

        public int Shade { get; } = shade;

        public Ground? ShownGround { get; set; }

        public ShownCrop? ShownCrop { get; set; }
    }

    private readonly record struct ShownCrop(Crop Crop, bool Ripe);

    private readonly Dictionary<(Color Color, bool Glow), StandardMaterial3D> _materials = [];
    private readonly List<Node3D> _rotors = [];
    private Node3D _tiles = null!;
    private Node3D _drone = null!;
    private MeshInstance3D _droneRing = null!;
    private Camera3D _camera = null!;
    private TileView[,] _tileViews = new TileView[0, 0];
    private float _elapsedSeconds;
    private Node3D _effects = null!;
    private Field? _shownField;
    private Dictionary<string, int> _shownItems = [];
    private Vector2 _droneTarget;
    private Vector2 _droneShown;
    private float _framingDistance;

    /// <summary>
    /// Creates the view. Its 3D scene is built when Godot readies the view.
    /// </summary>
    public FieldView3D()
    {
        Stretch = true;
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
        SubViewport viewport = new() { OwnWorld3D = true, TransparentBg = false, Msaa3D = Viewport.Msaa.Msaa4X };
        AddChild(viewport);

        _tiles = new Node3D();
        _effects = new Node3D();
        _drone = BuildDrone();
        _droneRing = BuildDroneRing();
        _camera = new Camera3D { Fov = 40 };
        DirectionalLight3D light = new()
        {
            ShadowEnabled = true,
            RotationDegrees = new Vector3(-35, -40, 0),
            LightColor = Palette.Sun,
            LightEnergy = 0.85f,
            ShadowBlur = 2.0f,
            ShadowOpacity = 0.7f,
        };
        WorldEnvironment worldEnvironment = new()
        {
            Environment = new Environment
            {
                BackgroundMode = Environment.BGMode.Sky,
                Sky = new Sky
                {
                    SkyMaterial = new ProceduralSkyMaterial
                    {
                        SkyTopColor = Palette.SkyTop,
                        SkyHorizonColor = Palette.SkyHorizon,
                        GroundHorizonColor = Palette.SkyGroundHorizon,
                        GroundBottomColor = Palette.SkyGroundBottom,
                        SkyCurve = 0.2f,
                    },
                },
                AmbientLightSource = Environment.AmbientSource.Color,
                AmbientLightColor = Palette.Ambient,
                AmbientLightEnergy = 0.5f,
                TonemapMode = Environment.ToneMapper.Filmic,
                TonemapExposure = 1.0f,
                FogEnabled = true,
                FogLightColor = Palette.Fog,
                FogDensity = 0.010f,
                FogSkyAffect = 0f,
                GlowEnabled = true,
                GlowIntensity = 0.5f,
                GlowBloom = 0f,
                GlowHdrThreshold = 0.9f,
            },
        };
        viewport.AddChild(_tiles);
        viewport.AddChild(_effects);
        viewport.AddChild(_drone);
        viewport.AddChild(_droneRing);
        viewport.AddChild(_camera);
        viewport.AddChild(light);
        viewport.AddChild(worldEnvironment);
        Resized += FrameField;
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
        float deltaSeconds = (float)delta;
        Vector2 previous = _droneShown;
        _droneShown = _droneShown.Lerp(_droneTarget, 1 - Mathf.Exp(-GlideRatePerSecond * deltaSeconds));
        // A zero-length frame has no velocity (0/0 gave a NaN tilt).
        Vector2 velocity = deltaSeconds > 0 ? (_droneShown - previous) / deltaSeconds : Vector2.Zero;
        Vector2 lean = velocity.Normalized() * Mathf.Min(velocity.Length() * LeanRadiansPerTilePerSecond, MaxLeanRadians);
        float hover = 0.9f + 0.05f * Mathf.Sin(_elapsedSeconds * 2);
        _drone.Position = new Vector3(_droneShown.X, hover, _droneShown.Y);
        _droneRing.Position = new Vector3(_droneShown.X, 0.02f, _droneShown.Y);
        _drone.Rotation = new Vector3(lean.Y, 0, -lean.X);
    }

    /// <summary>Gets the position of a crop in the game rules, which picks its colour and its shape.</summary>
    /// <param name="crop">The crop. It must be one of <paramref name="crops"/>.</param>
    /// <param name="crops">The crops of the game rules.</param>
    /// <returns>The crop's index in <paramref name="crops"/>.</returns>
    internal static int CropIndex(Crop crop, IReadOnlyList<Crop> crops)
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
                _tileViews[x, y] = BuildTileView(x, y);
            }
        }

        BuildWorld(width, height);

        Vector3 target = new((width - 1) / 2f, 0, (height - 1) / 2f);
        Vector3 direction = new Vector3(0, 1, 1).Normalized().Rotated(Vector3.Up, Mathf.DegToRad(35));
        _framingDistance = Mathf.Max(width, height) * 1.8f + 2;
        _camera.LookAtFromPosition(target + direction * _framingDistance, target, Vector3.Up);
        FrameField();
    }

    private void FrameField()
    {
        // The field is not built or laid out yet.
        if (_framingDistance == 0 || Size.Y == 0)
        {
            return;
        }

        float visibleHeight = 2 * _framingDistance * Mathf.Tan(Mathf.DegToRad(_camera.Fov / 2));
        float visibleWidth = visibleHeight * Size.X / Size.Y;
        _camera.HOffset = (0.5f - FieldCentreScreenFraction) * visibleWidth;
        _camera.VOffset = FieldLowerFraction * visibleHeight;
    }

    private void ShowTile(TileView view, Tile tile, IReadOnlyList<Crop> crops)
    {
        if (view.ShownGround != tile.Ground)
        {
            view.TileMesh.MaterialOverride = MaterialFor(Palette.Shaded(tile.Ground == Ground.Grass ? Palette.Grass : Palette.Soil, view.Shade), false);
            view.Furrows.Visible = tile.Ground == Ground.Soil;
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
        Color color = Palette.CropColor(crop, crops);
        Node3D shape = new();
        switch (CropIndex(crop, crops) % 3)
        {
            case 0:
                SphereMesh leafMesh = new() { Radius = 0.13f, Height = 0.05f };
                foreach (float angleDegrees in new[] { 90f, 210f, 330f })
                {
                    float angle = Mathf.DegToRad(angleDegrees);
                    shape.AddChild(Part(leafMesh, color, ripe, new Vector3(Mathf.Cos(angle) * 0.12f, 0.04f, Mathf.Sin(angle) * 0.12f)));
                }

                break;
            case 1:
                shape.AddChild(Part(new SphereMesh { Radius = 0.18f, Height = 0.36f }, color, ripe, new Vector3(0, 0.12f, 0)));
                PrismMesh leaf = new() { Size = new Vector3(0.08f, 0.2f, 0.02f) };
                MeshInstance3D leftLeaf = Part(leaf, Palette.Leaf, false, new Vector3(-0.08f, 0.34f, 0));
                leftLeaf.RotationDegrees = new Vector3(0, 0, 30);
                MeshInstance3D rightLeaf = Part(leaf, Palette.Leaf, false, new Vector3(0.08f, 0.34f, 0));
                rightLeaf.RotationDegrees = new Vector3(0, 0, -30);
                shape.AddChild(leftLeaf);
                shape.AddChild(rightLeaf);
                break;
            default:
                shape.AddChild(Part(new SphereMesh { Radius = 0.28f, Height = 0.36f }, Palette.Leaf, false, new Vector3(0, 0.18f, 0)));
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
        drone.AddChild(Part(new CylinderMesh { TopRadius = 0.18f, BottomRadius = 0.18f, Height = 0.12f }, Palette.DroneBody, false, Vector3.Zero));
        drone.AddChild(Part(new SphereMesh { Radius = 0.06f, Height = 0.12f }, Palette.DroneLight, true, new Vector3(0, 0.02f, -0.18f)));
        foreach (float armAngle in new[] { 45f, -45f })
        {
            MeshInstance3D arm = Part(new BoxMesh { Size = new Vector3(0.5f, 0.03f, 0.04f) }, Palette.DroneBody, false, new Vector3(0, 0.03f, 0));
            arm.RotationDegrees = new Vector3(0, armAngle, 0);
            drone.AddChild(arm);
        }

        CylinderMesh rotorMesh = new() { TopRadius = 0.12f, BottomRadius = 0.12f, Height = 0.01f };
        foreach (Vector3 position in new[] { new Vector3(0.18f, 0.06f, 0.18f), new Vector3(-0.18f, 0.06f, 0.18f), new Vector3(0.18f, 0.06f, -0.18f), new Vector3(-0.18f, 0.06f, -0.18f) })
        {
            MeshInstance3D rotor = Part(rotorMesh, Palette.Rotor, false, position);
            drone.AddChild(rotor);
            _rotors.Add(rotor);
        }

        return drone;
    }
}
