using Godot;

namespace CodingGame.Godot;

public partial class FieldView3D
{
    private const float ShakeSeconds = 0.4f;
    private const float ShakeAmplitudeTiles = 0.06f;

    private Label3D _commandLabel = null!;
    private bool _shownFailed;
    private float _shakeLeftSeconds;

    private static Label3D BuildCommandLabel()
    {
        return new Label3D
        {
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 40,
            OutlineSize = 8,
            NoDepthTest = true,
            PixelSize = 0.004f,
            Position = new Vector3(0, 0.55f, 0),
            Visible = false,
        };
    }

    /// <summary>
    /// Links the run to the drone: the command on the running line floats over the drone, and when the run stops with
    /// an error the drone shakes once and gives off a red burst. Call it every frame. It only reads.
    /// </summary>
    /// <param name="runningCommand">The text of the line that is running, or null when no run is going.</param>
    /// <param name="failed">True when the run stopped with an error.</param>
    public void ShowRun(string? runningCommand, bool failed)
    {
        _commandLabel.Text = runningCommand ?? "";
        _commandLabel.Visible = runningCommand is not null;
        if (failed && !_shownFailed)
        {
            _shakeLeftSeconds = ShakeSeconds;
            SpawnBurst(_drone.Position, Palette.ErrorText, 20, 1f, 2f);
        }

        _shownFailed = failed;
    }

    private float ShakeOffset(float deltaSeconds)
    {
        // A fast wobble whose amplitude fades with the time left, so the shake dies out instead of snapping off.
        _shakeLeftSeconds = Mathf.Max(0f, _shakeLeftSeconds - deltaSeconds);
        return ShakeAmplitudeTiles * Mathf.Sin(_shakeLeftSeconds * 60f) * (_shakeLeftSeconds / ShakeSeconds);
    }
}
