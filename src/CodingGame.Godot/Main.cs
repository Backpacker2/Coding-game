using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using CodingGame.Core.Simulation;
using Godot;
using ScriptLanguage = CodingGame.Core.Interpreter.ScriptLanguage;

namespace CodingGame.Godot;

/// <summary>
/// The game screen: the farm fills it, with the script window floating over it, the item bar at the top left
/// and round buttons at the top right that open the Unlocks, Help and Menu overlays.
/// </summary>
public partial class Main : Control
{
    // The controls and the rules are created in _Ready, after Godot has built the object.
    private GameRules _rules = null!;
    private GameProgress _progress = null!;
    private DroneApi _api = null!;
    private FieldView3D _fieldView = null!;
    private ResourceBar _resourceBar = null!;
    private CodeWindow _codeWindow = null!;
    private UnlockPanel _unlockPanel = null!;
    private HelpPanel _helpPanel = null!;
    private HelpBook _help = null!;
    private MenuButton _menuButton = null!;
    private ConfirmationDialog _newGameDialog = null!;

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
        _codeWindow = new CodeWindow();
        AddChild(_codeWindow);
        SavedGame saved = LoadSavedGame();
        _progress = saved.Progress;
        _language = saved.Language;
        _scriptTexts[ScriptLanguage.Python] = saved.Scripts.Python ?? StarterText(ScriptLanguage.Python);
        _scriptTexts[ScriptLanguage.JavaScript] = saved.Scripts.JavaScript ?? StarterText(ScriptLanguage.JavaScript);
        _api = new DroneApi(_rules, _progress.Field);
        _help = HelpBook.Load(DataFolder, _api);

        _codeWindow.Editor.Text = _scriptTexts[_language];
        _codeWindow.Editor.UseLanguage(_language, _api);
        _codeWindow.SelectLanguage(_language);
        _codeWindow.RunPressed += OnRun;
        _codeWindow.StepPressed += OnStep;
        _codeWindow.StopPressed += OnStop;
        _codeWindow.LanguageSelected += OnLanguageSelected;
        Vector2 viewport = GetViewportRect().Size;
        _codeWindow.Position = new Vector2I((int)(viewport.X * 0.55f), 80);
        _codeWindow.Size = new Vector2I((int)(viewport.X * 0.42f), (int)(viewport.Y * 0.8f));

        _fieldView = new FieldView3D();
        _fieldView.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_fieldView);

        _resourceBar = new ResourceBar { Position = new Vector2(16, 12) };
        AddChild(_resourceBar);

        _unlockPanel = new UnlockPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _unlockPanel.Show(_progress);
        _unlockPanel.Bought += OnBought;
        ScrollContainer unlockScroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        unlockScroll.AddChild(_unlockPanel);
        Window unlocksWindow = OverlayWindow("Unlocks", new Vector2I(520, 480));
        unlocksWindow.AddChild(unlockScroll);
        unlockScroll.SetAnchorsPreset(LayoutPreset.FullRect);

        _helpPanel = new HelpPanel();
        Window helpWindow = OverlayWindow("Help", new Vector2I(560, 520));
        helpWindow.AddChild(_helpPanel);
        _helpPanel.SetAnchorsPreset(LayoutPreset.FullRect);

        _newGameDialog = new ConfirmationDialog
        {
            DialogText = "Start a new game? Your items, unlocks and scripts are erased.",
            OkButtonText = "Erase and start over",
            CancelButtonText = "Keep playing",
        };
        _newGameDialog.Confirmed += OnNewGameConfirmed;
        AddChild(_newGameDialog);

        Button codeButton = new() { Text = "Code" };
        ApplyRoundStyle(codeButton);
        codeButton.Pressed += () => ToggleWindow(_codeWindow);
        Button unlocksButton = new() { Text = "Unlocks" };
        ApplyRoundStyle(unlocksButton);
        unlocksButton.Pressed += () => ToggleWindow(unlocksWindow);
        Button helpButton = new() { Text = "Help" };
        ApplyRoundStyle(helpButton);
        helpButton.Pressed += () => ToggleWindow(helpWindow);
        // A menu button is flat by default, which hides the round background.
        _menuButton = new MenuButton { Text = "Menu", Flat = false };
        ApplyRoundStyle(_menuButton);
        PopupMenu menu = _menuButton.GetPopup();
        menu.AddItem("New game");
        menu.IdPressed += _ => _newGameDialog.PopupCentered();

        HBoxContainer roundButtons = new() { GrowHorizontal = GrowDirection.Begin };
        roundButtons.SetAnchorsPreset(LayoutPreset.TopRight);
        roundButtons.OffsetTop = 12;
        roundButtons.OffsetRight = -16;
        roundButtons.AddThemeConstantOverride("separation", 8);
        roundButtons.AddChild(codeButton);
        roundButtons.AddChild(unlocksButton);
        roundButtons.AddChild(helpButton);
        roundButtons.AddChild(_menuButton);
        AddChild(roundButtons);
        RefreshHelp();
    }

    /// <inheritdoc/>
    public override void _Process(double delta)
    {
        if (_playing && _session?.Status == StepStatus.Running)
        {
            _tickBudget += delta * _codeWindow.TicksPerSecond;
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

    private static void ApplyRoundStyle(Button button)
    {
        button.CustomMinimumSize = new Vector2(48, 48);
        button.AddThemeStyleboxOverride("normal", RoundStyle(new Color(0.25f, 0.45f, 0.2f)));
        button.AddThemeStyleboxOverride("hover", RoundStyle(new Color(0.31f, 0.55f, 0.25f)));
        button.AddThemeStyleboxOverride("pressed", RoundStyle(new Color(0.18f, 0.34f, 0.15f)));
    }

    private static StyleBoxFlat RoundStyle(Color background)
    {
        StyleBoxFlat style = new() { BgColor = background };
        style.SetCornerRadiusAll(24);
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        return style;
    }

    private static void ToggleWindow(Window window)
    {
        if (window.Visible)
        {
            window.Hide();
            return;
        }

        window.Show();
        window.GrabFocus();
    }

    private Window OverlayWindow(string title, Vector2I size)
    {
        Vector2I viewport = (Vector2I)GetViewportRect().Size;
        Window window = new()
        {
            Title = title,
            Size = size,
            Position = (viewport - size) / 2,
            Visible = false,
        };
        window.CloseRequested += window.Hide;
        AddChild(window);
        return window;
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
            _codeWindow.ShowNotice($"Could not read the save file ({exception.Message}). It was kept as {keptPath}. Starting a new game.");
            return new SavedGame(new GameProgress(_rules), ScriptLanguage.Python, new SavedScripts(null, null));
        }
    }

    private void Save()
    {
        _scriptTexts[_language] = _codeWindow.Editor.Text;
        SaveFile.Save(SavePath, new SavedGame(
            _progress,
            _language,
            new SavedScripts(_scriptTexts[ScriptLanguage.Python], _scriptTexts[ScriptLanguage.JavaScript])));
    }

    private RunSession SessionForRun()
    {
        if (_session is null || _session.Status != StepStatus.Running)
        {
            _session = RunSession.Start(_codeWindow.Editor.Text, _language, _progress);
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
    }

    private void OnBought()
    {
        RefreshHelp();
        Save();
    }

    private void OnLanguageSelected(ScriptLanguage next)
    {
        _scriptTexts[_language] = _codeWindow.Editor.Text;
        _language = next;
        _codeWindow.Editor.Text = _scriptTexts[next];
        _codeWindow.Editor.UseLanguage(_language, _api);
        RefreshHelp();
    }

    private void OnNewGameConfirmed()
    {
        _progress.Reset();
        _scriptTexts[ScriptLanguage.Python] = StarterText(ScriptLanguage.Python);
        _scriptTexts[ScriptLanguage.JavaScript] = StarterText(ScriptLanguage.JavaScript);
        _language = ScriptLanguage.Python;
        _codeWindow.SelectLanguage(_language);
        _codeWindow.Editor.Text = _scriptTexts[_language];
        _codeWindow.Editor.UseLanguage(_language, _api);
        _session = null;
        _playing = false;
        _tickBudget = 0;
        _codeWindow.ShowNotice("");
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
        Field field = _progress.Field;
        _fieldView.ShowField(field, _rules.Crops);
        _resourceBar.ShowItems(field.Items, _rules.Crops);
        _codeWindow.Refresh(session);

        bool running = session?.Status == StepStatus.Running;
        _menuButton.GetPopup().SetItemDisabled(0, running);
        _unlockPanel.Refresh(running);
    }
}
