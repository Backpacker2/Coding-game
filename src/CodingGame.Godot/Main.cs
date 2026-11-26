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
    // Ticks per second for Slow, Normal and Fast. A move costs 20 ticks, so Normal moves the drone about
    // once a second. Slow lets the player follow each step; Fast finishes long loops in seconds.
    private static readonly double[] _ticksPerSecondBySpeed = [5, 20, 200];

    // The controls and the rules are created in _Ready, after Godot has built the object.
    private GameRules _rules = null!;
    private GameProgress _progress = null!;
    private Field _idleField = null!;
    private FarmView _farm = null!;
    private ScriptEditor _editor = null!;
    private OptionButton _languagePicker = null!;
    private OptionButton _speedPicker = null!;
    private Button _stopButton = null!;
    private Label _tickLabel = null!;
    private Label _itemsLabel = null!;
    private Label _errorLabel = null!;
    private UnlockPanel _unlockPanel = null!;
    private HelpPanel _helpPanel = null!;
    private HelpBook _help = null!;
    private Button _newGameButton = null!;
    private ConfirmationDialog _newGameDialog = null!;
    private Label _noticeLabel = null!;

    private RunSession? _session;
    private bool _playing;
    private bool _wasRunning;
    private double _tickBudget;
    private ScriptLanguage _language = ScriptLanguage.Python;
    private readonly Dictionary<ScriptLanguage, string> _scriptTexts = [];

    /// <inheritdoc/>
    public override void _Ready()
    {
        _rules = GameRules.Load(DataFolder);
        _noticeLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _noticeLabel.AddThemeColorOverride("font_color", Colors.Red);
        SavedGame saved = LoadSavedGame();
        _progress = saved.Progress;
        _language = saved.Language;
        _scriptTexts[ScriptLanguage.Python] = saved.Scripts.Python ?? StarterText(ScriptLanguage.Python);
        _scriptTexts[ScriptLanguage.JavaScript] = saved.Scripts.JavaScript ?? StarterText(ScriptLanguage.JavaScript);
        _idleField = _progress.NewField();
        _help = HelpBook.Load(DataFolder, new DroneApi(_rules, _idleField));

        _languagePicker = new OptionButton();
        _languagePicker.AddItem("Python");
        _languagePicker.AddItem("JavaScript");
        _languagePicker.Select((int)_language);
        _languagePicker.ItemSelected += OnLanguageSelected;

        Button runButton = new() { Text = "Run" };
        runButton.Pressed += OnRun;
        Button stepButton = new() { Text = "Step" };
        stepButton.Pressed += OnStep;
        _stopButton = new Button { Text = "Stop" };
        _stopButton.Pressed += OnStop;
        _speedPicker = new OptionButton();
        _speedPicker.AddItem("Slow");
        _speedPicker.AddItem("Normal");
        _speedPicker.AddItem("Fast");
        _speedPicker.Select(1);
        _tickLabel = new Label();
        Button helpButton = new() { Text = "Help", ToggleMode = true, ButtonPressed = true };
        helpButton.Toggled += pressed => _helpPanel.Visible = pressed;
        _newGameButton = new Button { Text = "New game" };
        _newGameButton.Pressed += () => _newGameDialog.PopupCentered();
        _newGameDialog = new ConfirmationDialog
        {
            DialogText = "Start a new game? Your items, unlocks and scripts are erased.",
            OkButtonText = "Erase and start over",
            CancelButtonText = "Keep playing",
        };
        _newGameDialog.Confirmed += OnNewGameConfirmed;
        AddChild(_newGameDialog);

        HBoxContainer toolbar = new();
        toolbar.AddChild(_languagePicker);
        toolbar.AddChild(runButton);
        toolbar.AddChild(stepButton);
        toolbar.AddChild(_stopButton);
        toolbar.AddChild(_speedPicker);
        toolbar.AddChild(_tickLabel);
        toolbar.AddChild(helpButton);
        toolbar.AddChild(_newGameButton);

        _editor = new ScriptEditor { Text = _scriptTexts[_language] };

        _errorLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _errorLabel.AddThemeColorOverride("font_color", Colors.Red);
        PanelContainer errorPanel = new() { CustomMinimumSize = new Vector2(0, 80) };
        errorPanel.AddChild(_errorLabel);

        VBoxContainer left = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        left.AddChild(toolbar);
        left.AddChild(_editor);
        left.AddChild(_noticeLabel);
        left.AddChild(errorPanel);

        _farm = new FarmView { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        _itemsLabel = new Label();
        _unlockPanel = new UnlockPanel();
        _unlockPanel.Show(_progress);
        _unlockPanel.Bought += OnBought;
        _helpPanel = new HelpPanel();

        VBoxContainer right = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        right.AddChild(_farm);
        right.AddChild(_itemsLabel);
        right.AddChild(_unlockPanel);
        right.AddChild(_helpPanel);

        HSplitContainer split = new();
        split.SetAnchorsPreset(LayoutPreset.FullRect);
        split.AddChild(left);
        split.AddChild(right);
        AddChild(split);
        RefreshHelp();
    }

    /// <inheritdoc/>
    public override void _Process(double delta)
    {
        if (_playing && _session?.Status == StepStatus.Running)
        {
            _tickBudget += delta * _ticksPerSecondBySpeed[_speedPicker.Selected];
        }

        while (_tickBudget >= 1 && _session is { Status: StepStatus.Running })
        {
            _session.StepTick();
            _tickBudget -= 1;
        }

        bool running = _session?.Status == StepStatus.Running;
        if (_wasRunning && !running)
        {
            Save();
        }

        _wasRunning = running;
        Refresh();
    }

    /// <inheritdoc/>
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            Save();
        }
    }

    private static string DataFolder => ProjectSettings.GlobalizePath("res://data");

    private static string SavePath => ProjectSettings.GlobalizePath("user://save.json");

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

    private SavedGame LoadSavedGame()
    {
        try
        {
            return SaveFile.Load(SavePath, _rules);
        }
        catch (SaveException exception)
        {
            string keptPath = SaveFile.SetAside(SavePath);
            _noticeLabel.Text = $"Could not read the save file ({exception.Message}). It was kept as {keptPath}. Starting a new game.";
            return new SavedGame(new GameProgress(_rules), ScriptLanguage.Python, new SavedScripts(null, null));
        }
    }

    private void Save()
    {
        _scriptTexts[_language] = _editor.Text;
        SaveFile.Save(SavePath, new SavedGame(
            _progress,
            _language,
            new SavedScripts(_scriptTexts[ScriptLanguage.Python], _scriptTexts[ScriptLanguage.JavaScript])));
    }

    private RunSession SessionForRun()
    {
        if (_session is null || _session.Status != StepStatus.Running)
        {
            _session = RunSession.Start(_editor.Text, _language, _progress);
            _tickBudget = 0;
            Save();
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

        // A step can start and end a run within one frame, which the save check in _Process never sees.
        if (session.Status != StepStatus.Running)
        {
            Save();
        }
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
        RefreshHelp();
        Save();
    }

    private void OnLanguageSelected(long index)
    {
        ScriptLanguage next = (ScriptLanguage)index;
        _scriptTexts[_language] = _editor.Text;
        _language = next;
        _editor.Text = _scriptTexts[next];
        RefreshHelp();
    }

    private void OnNewGameConfirmed()
    {
        _progress.Reset();
        _scriptTexts[ScriptLanguage.Python] = StarterText(ScriptLanguage.Python);
        _scriptTexts[ScriptLanguage.JavaScript] = StarterText(ScriptLanguage.JavaScript);
        _language = ScriptLanguage.Python;
        _languagePicker.Select((int)_language);
        _editor.Text = _scriptTexts[_language];
        _session = null;
        _playing = false;
        _tickBudget = 0;
        _noticeLabel.Text = "";
        _idleField = _progress.NewField();
        RefreshHelp();
        Save();
    }

    private void RefreshHelp()
    {
        _helpPanel.ShowEntries(_help.EntriesFor(_progress, _language));
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
        _newGameButton.Disabled = running;
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
