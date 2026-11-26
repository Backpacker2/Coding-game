using Godot;

namespace CodingGame.Godot;

public partial class FieldView3D
{
    private const float DefaultYawRadians = 35f * Mathf.Pi / 180f;
    private const float DefaultPitchRadians = Mathf.Pi / 4;

    // Lower shows mostly sky; higher looks straight down and hides the crops' shapes.
    private const float MinPitchRadians = 25f * Mathf.Pi / 180f;
    private const float MaxPitchRadians = 75f * Mathf.Pi / 180f;

    // Further out than 1.5 the field gets too small to read; closer than 0.55 it leaves the screen.
    private const float MinZoom = 0.55f;
    private const float MaxZoom = 1.5f;

    private const float OrbitRadiansPerPixel = 0.008f;
    private const float ZoomStep = 1.1f;
    private const float CameraEaseRatePerSecond = 10f;

    private Vector3 _cameraTarget;
    private float _yawGoal = DefaultYawRadians;
    private float _pitchGoal = DefaultPitchRadians;
    private float _zoomGoal = 1f;
    private float _yawShown = DefaultYawRadians;
    private float _pitchShown = DefaultPitchRadians;
    private float _zoomShown = 1f;

    /// <summary>
    /// Turns the camera back to the default view, smoothly.
    /// </summary>
    public void ResetCamera()
    {
        _yawGoal = DefaultYawRadians;
        _pitchGoal = DefaultPitchRadians;
        _zoomGoal = 1f;
    }

    /// <summary>
    /// Lets the camera follow right-drag and scroll over the field while <paramref name="window"/> has focus.
    /// A focused window gets all mouse input, also outside its own area, so without this the camera would
    /// not move until the player clicks the field.
    /// </summary>
    /// <param name="window">A window that floats over the field.</param>
    public void FollowMouseAround(Window window)
    {
        window.WindowInput += @event =>
        {
            if (@event is InputEventMouse mouse && !new Rect2(Vector2.Zero, window.Size).HasPoint(mouse.Position))
            {
                TurnCamera(mouse);
            }
        };
    }

    /// <inheritdoc/>
    public override void _GuiInput(InputEvent @event)
    {
        if (TurnCamera(@event))
        {
            AcceptEvent();
        }
    }

    // Returns whether the event moved the camera.
    private bool TurnCamera(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && (motion.ButtonMask & MouseButtonMask.Right) != 0)
        {
            _yawGoal -= motion.Relative.X * OrbitRadiansPerPixel;
            _pitchGoal = Mathf.Clamp(_pitchGoal + motion.Relative.Y * OrbitRadiansPerPixel, MinPitchRadians, MaxPitchRadians);
            return true;
        }

        if (@event is InputEventMouseButton { Pressed: true } button)
        {
            switch (button.ButtonIndex)
            {
                case MouseButton.WheelUp:
                    _zoomGoal = Mathf.Max(_zoomGoal / ZoomStep, MinZoom);
                    return true;
                case MouseButton.WheelDown:
                    _zoomGoal = Mathf.Min(_zoomGoal * ZoomStep, MaxZoom);
                    return true;
            }
        }

        return false;
    }

    private void PlaceCamera(float deltaSeconds)
    {
        // Before the field is built the target is the camera's own position, which Godot reports as an error.
        if (_framingDistance == 0)
        {
            return;
        }

        // Exponential easing: fast at first, then settling, the same on any frame rate.
        float ease = 1 - Mathf.Exp(-CameraEaseRatePerSecond * deltaSeconds);
        _yawShown = Mathf.Lerp(_yawShown, _yawGoal, ease);
        _pitchShown = Mathf.Lerp(_pitchShown, _pitchGoal, ease);
        _zoomShown = Mathf.Lerp(_zoomShown, _zoomGoal, ease);
        Vector3 direction = new Vector3(0, Mathf.Sin(_pitchShown), Mathf.Cos(_pitchShown)).Rotated(Vector3.Up, _yawShown);
        _camera.LookAtFromPosition(_cameraTarget + direction * _framingDistance * _zoomShown, _cameraTarget, Vector3.Up);
        FrameField();
    }
}
