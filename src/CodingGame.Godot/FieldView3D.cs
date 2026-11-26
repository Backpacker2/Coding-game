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

    // One turn in 0.6 seconds: quick enough to finish before a following move at Normal speed.
    private const float SpinRadiansPerSecond = Mathf.Tau / 0.6f;

    // The script window starts at 55% of the screen width (Main), so the field is centred in the left 55%.
    internal const float FieldCentreScreenFraction = 0.275f;

    // Leaves room for the item bar at the top.
    private const float FieldLowerFraction = 0.04f;

    private sealed class TileView(MeshInstance3D tileMesh, Node3D cropSlot, Node3D furrows, int shade, float swayPhaseRadians)
    {
        public MeshInstance3D TileMesh { get; } = tileMesh;

        public Node3D CropSlot { get; } = cropSlot;

        public Node3D Furrows { get; } = furrows;

        public int Shade { get; } = shade;

        public float SwayPhaseRadians { get; } = swayPhaseRadians;

        public Ground? ShownGround { get; set; }

        public ShownCrop? ShownCrop { get; set; }
    }

    private readonly record struct ShownCrop(Crop Crop, GrowthStage Stage);

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
    private int _shownSpinCount;
    private float _spinLeftRadians;

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

        // A new field starts its spin count at 0, so only spins on the same field turn the drone.
        if (!newField)
        {
            _spinLeftRadians += Mathf.Tau * (field.SpinCount - _shownSpinCount);
        }

        _shownSpinCount = field.SpinCount;
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
        _commandLabel = BuildCommandLabel();
        _drone.AddChild(_commandLabel);
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
        PlaceCamera((float)delta);
        SwayRipeCrops();
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
        _drone.Position = new Vector3(_droneShown.X + ShakeOffset(deltaSeconds), hover, _droneShown.Y);
        _droneRing.Position = new Vector3(_droneShown.X, 0.02f, _droneShown.Y);
        _spinLeftRadians = Mathf.MoveToward(_spinLeftRadians, 0, SpinRadiansPerSecond * deltaSeconds);
        _drone.Rotation = new Vector3(lean.Y, _spinLeftRadians, -lean.X);
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

        _cameraTarget = new Vector3((width - 1) / 2f, 0, (height - 1) / 2f);
        _framingDistance = Mathf.Max(width, height) * 1.8f + 2;
        FrameField();
    }

    private void FrameField()
    {
        // The field is not built or laid out yet.
        if (_framingDistance == 0 || Size.Y == 0)
        {
            return;
        }

        float visibleHeight = 2 * _framingDistance * _zoomShown * Mathf.Tan(Mathf.DegToRad(_camera.Fov / 2));
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

        GrowthStage stage = StageOf(tile.CropAgeTicks, tile.Crop.GrowTimeTicks);
        ShownCrop shown = new(tile.Crop, stage);
        if (view.ShownCrop != shown)
        {
            FreeChildren(view.CropSlot);
            // The tween runs on the shape, not the slot, because ShowTile sets the slot's scale every frame.
            Node3D shape = BuildCropShape(tile.Crop, stage, crops);
            view.CropSlot.AddChild(shape);
            shape.Scale = Vector3.One * PopInStartScale;
            shape.CreateTween().TweenProperty(shape, "scale", Vector3.One, PopInSeconds)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            view.CropSlot.Rotation = Vector3.Zero;
            view.ShownCrop = shown;
        }

        view.CropSlot.Visible = true;
        view.CropSlot.Scale = Vector3.One * GrowthScale(stage, tile.CropAgeTicks, tile.Crop.GrowTimeTicks);
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
