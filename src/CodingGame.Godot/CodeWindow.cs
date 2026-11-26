using CodingGame.Core.Interpreter;
using CodingGame.Core.Session;
using Godot;
using ScriptLanguage = CodingGame.Core.Interpreter.ScriptLanguage;

namespace CodingGame.Godot;

/// <summary>
/// The floating script window: the language picker, Run, Step and Stop, the speed picker, the tick count,
/// the script editor and the Messages box. Closing it hides the window; the Code button shows it again.
/// </summary>
public partial class CodeWindow : Window
{
    // Ticks per second for Slow, Normal and Fast. A move costs 20 ticks, so Normal moves the drone about
    // once a second. Slow lets the player follow each step; Fast finishes long loops in seconds.
    private static readonly double[] _ticksPerSecondBySpeed = [5, 20, 200];

    private OptionButton _languagePicker = null!;
    private OptionButton _speedPicker = null!;
    private Button _stopButton = null!;
    private Label _tickLabel = null!;
    private Label _errorLabel = null!;
    private Label _noticeLabel = null!;

    /// <summary>Raised when the player presses Run.</summary>
    public event Action? RunPressed;

    /// <summary>Raised when the player presses Step.</summary>
    public event Action? StepPressed;

    /// <summary>Raised when the player presses Stop.</summary>
    public event Action? StopPressed;

    /// <summary>Raised when the player picks another language in the language picker.</summary>
    public event Action<ScriptLanguage>? LanguageSelected;

    /// <summary>Gets the script editor, which holds the text of the script being edited.</summary>
    public ScriptEditor Editor { get; } = new();

    /// <summary>
    /// Gets the speed of a run in ticks per second, as chosen in the speed picker.
    /// </summary>
    public double TicksPerSecond => _ticksPerSecondBySpeed[_speedPicker.Selected];

    /// <summary>Creates the script window with its title and minimum size. Its controls are built when it enters the tree.</summary>
    public CodeWindow()
    {
        Title = "Script";
        MinSize = new Vector2I(420, 300);
    }

    /// <summary>Sets the language picker to a language without raising <see cref="LanguageSelected"/>.</summary>
    /// <param name="language">The language to show.</param>
    public void SelectLanguage(ScriptLanguage language)
    {
        _languagePicker.Select((int)language);
    }

    /// <summary>Shows a red notice above the Messages box. An empty text clears it.</summary>
    /// <param name="text">The notice to show.</param>
    public void ShowNotice(string text)
    {
        _noticeLabel.Text = text;
    }

    /// <summary>
    /// Shows the state of a run: the tick count and its status, the error or the hint in the Messages box,
    /// whether the editor and the language picker are locked, and the line the run is on.
    /// </summary>
    /// <param name="session">The run to show, or null when no run has started.</param>
    public void Refresh(RunSession? session)
    {
        string statusText = session?.Status switch
        {
            StepStatus.Finished => " - finished",
            StepStatus.Failed => " - stopped by an error",
            _ => "",
        };
        _tickLabel.Text = $"Ticks: {session?.TicksUsed ?? 0}{statusText}";

        string? error = session?.ErrorMessage;
        _errorLabel.Text = error ?? "Errors in your script appear here, with the line number.";
        _errorLabel.AddThemeColorOverride("font_color", error is null ? Palette.MutedText : Palette.ErrorText);
        bool running = session?.Status == StepStatus.Running;
        Editor.Editable = !running;
        _stopButton.Disabled = session is null;
        _languagePicker.Disabled = running;

        int line = session?.Error?.Line ?? session?.CurrentLine ?? 0;
        if (session is null || line == 0)
        {
            Editor.ClearHighlight();
            return;
        }

        Color color = session.Status == StepStatus.Failed
            ? Palette.ErrorLineHighlight
            : Palette.RunningLineHighlight;
        Editor.HighlightLine(line, color);
    }

    /// <inheritdoc/>
    public override void _Ready()
    {
        CloseRequested += Hide;

        _noticeLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _noticeLabel.AddThemeColorOverride("font_color", Palette.ErrorText);

        _languagePicker = new OptionButton();
        _languagePicker.AddItem("Python");
        _languagePicker.AddItem("JavaScript");
        _languagePicker.ItemSelected += index => LanguageSelected?.Invoke((ScriptLanguage)index);

        Button runButton = new() { Text = "Run" };
        runButton.Pressed += () => RunPressed?.Invoke();
        Button stepButton = new() { Text = "Step" };
        stepButton.Pressed += () => StepPressed?.Invoke();
        _stopButton = new Button { Text = "Stop" };
        _stopButton.Pressed += () => StopPressed?.Invoke();
        _speedPicker = new OptionButton();
        _speedPicker.AddItem("Slow");
        _speedPicker.AddItem("Normal");
        _speedPicker.AddItem("Fast");
        _speedPicker.Select(1);
        _tickLabel = new Label();

        HBoxContainer toolbar = new();
        toolbar.AddChild(_languagePicker);
        toolbar.AddChild(runButton);
        toolbar.AddChild(stepButton);
        toolbar.AddChild(_stopButton);
        toolbar.AddChild(_speedPicker);
        toolbar.AddChild(_tickLabel);

        _errorLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        PanelContainer errorPanel = new() { CustomMinimumSize = new Vector2(0, 80) };
        errorPanel.AddChild(_errorLabel);

        VBoxContainer column = new();
        column.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        column.AddChild(toolbar);
        column.AddChild(Editor);
        column.AddChild(_noticeLabel);
        column.AddChild(new Label { Text = "Messages" });
        column.AddChild(errorPanel);
        AddChild(column);
    }
}
