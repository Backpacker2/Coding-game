using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using CodingGame.Core.Simulation;
using Godot;
using ScriptLanguage = CodingGame.Core.Interpreter.ScriptLanguage;

namespace CodingGame.Godot;

/// <summary>The game window: the script editor, the controls and the farm view.</summary>
public partial class Main : Control
{
    // A move costs 20 ticks, so the drone moves about once a second. A speed slider comes later.
    private const double TicksPerSecond = 20;

    // The controls and the rules are created in _Ready, after Godot has built the object.
    private GameRules _rules = null!;
    private GameProgress _progress = null!;
    private Field _idleField = null!;
    private FarmView _farm = null!;
    private ScriptEditor _editor = null!;
    private OptionButton _languagePicker = null!;
    private Button _stopButton = null!;
    private Label _tickLabel = null!;
    private Label _itemsLabel = null!;
    private Label _errorLabel = null!;
    private UnlockPanel _unlockPanel = null!;

    private RunSession? _session;
    private bool _playing;
    private double _tickBudget;
    private ScriptLanguage _language = ScriptLanguage.Python;

    /// <inheritdoc/>
    public override void _Ready()
    {
        _rules = GameRules.Load(DataFolder);
        _progress = new GameProgress(_rules);
        _idleField = _progress.NewField();

        _languagePicker = new OptionButton();
        _languagePicker.AddItem("Python");
        _languagePicker.AddItem("JavaScript");
        _languagePicker.ItemSelected += OnLanguageSelected;

        Button runButton = new() { Text = "Run" };
        runButton.Pressed += OnRun;
        Button stepButton = new() { Text = "Step" };
        stepButton.Pressed += OnStep;
        _stopButton = new Button { Text = "Stop" };
        _stopButton.Pressed += OnStop;
        _tickLabel = new Label();

        HBoxContainer toolbar = new();
        toolbar.AddChild(_languagePicker);
        toolbar.AddChild(runButton);
        toolbar.AddChild(stepButton);
        toolbar.AddChild(_stopButton);
        toolbar.AddChild(_tickLabel);

        _editor = new ScriptEditor { Text = StarterText(_language) };

        _errorLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _errorLabel.AddThemeColorOverride("font_color", Colors.Red);
        PanelContainer errorPanel = new() { CustomMinimumSize = new Vector2(0, 80) };
        errorPanel.AddChild(_errorLabel);

        VBoxContainer left = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        left.AddChild(toolbar);
        left.AddChild(_editor);
        left.AddChild(errorPanel);

        _farm = new FarmView { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        _itemsLabel = new Label();
        _unlockPanel = new UnlockPanel();
        _unlockPanel.Show(_progress);
        _unlockPanel.Bought += OnBought;

        VBoxContainer right = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        right.AddChild(_farm);
        right.AddChild(_itemsLabel);
        right.AddChild(_unlockPanel);

        HSplitContainer split = new();
        split.SetAnchorsPreset(LayoutPreset.FullRect);
        split.AddChild(left);
        split.AddChild(right);
        AddChild(split);
    }

    /// <inheritdoc/>
    public override void _Process(double delta)
    {
        if (_playing && _session?.Status == StepStatus.Running)
        {
            _tickBudget += delta * TicksPerSecond;
        }

        while (_tickBudget >= 1 && _session is { Status: StepStatus.Running })
        {
            _session.StepTick();
            _tickBudget -= 1;
        }

        Refresh();
    }

    private static string DataFolder => ProjectSettings.GlobalizePath("res://data");

    private static string StarterText(ScriptLanguage language)
    {
        string fileName = language switch
        {
            ScriptLanguage.Python => "starter.py",
            ScriptLanguage.JavaScript => "starter.js",
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, "unknown script language"),
        };
        return File.ReadAllText(Path.Combine(DataFolder, fileName));
    }

    private RunSession SessionForRun()
    {
        if (_session is null || _session.Status != StepStatus.Running)
        {
            _session = RunSession.Start(_editor.Text, _language, _progress);
            _tickBudget = 0;
        }

        return _session;
    }

    private void OnRun()
    {
        SessionForRun();
        _playing = true;
    }

    private void OnStep()
    {
        RunSession session = SessionForRun();
        _playing = false;
        session.StepLine();
    }

    private void OnStop()
    {
        _session = null;
        _playing = false;
        _idleField = _progress.NewField();
    }

    private void OnBought()
    {
        _idleField = _progress.NewField();
    }

    private void OnLanguageSelected(long index)
    {
        ScriptLanguage next = (ScriptLanguage)index;
        if (_editor.Text == StarterText(_language))
        {
            _editor.Text = StarterText(next);
        }

        _language = next;
    }

    private void Refresh()
    {
        RunSession? session = _session;
        Field field = session?.Field ?? _idleField;
        _farm.ShowField(field, _rules.Crops);

        string statusText = session?.Status switch
        {
            StepStatus.Finished => " - finished",
            StepStatus.Failed => " - stopped by an error",
            _ => "",
        };
        _tickLabel.Text = $"Ticks: {session?.TicksUsed ?? 0}{statusText}";

        string items = string.Join(", ", field.Items.Select(item => $"{item.Key} {item.Value}"));
        _itemsLabel.Text = items.Length == 0 ? "Items: none" : $"Items: {items}";

        _errorLabel.Text = session?.ErrorMessage ?? "";
        bool running = session?.Status == StepStatus.Running;
        _editor.Editable = !running;
        _stopButton.Disabled = session is null;
        _languagePicker.Disabled = running;
        _unlockPanel.Refresh(running);

        int line = session?.Error?.Line ?? session?.CurrentLine ?? 0;
        if (session is null || line == 0)
        {
            _editor.ClearHighlight();
            return;
        }

        Color color = session.Status == StepStatus.Failed
            ? new Color(1, 0, 0, 0.35f)
            : new Color(1, 1, 0, 0.25f);
        _editor.HighlightLine(line, color);
    }
}
