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
    private ScriptBook _scripts = null!;
    private DroneApi _api = null!;
    private FieldView3D _fieldView = null!;
    private ResourceBar _resourceBar = null!;
    private GoalBar _goalBar = null!;
    private GoalCard _goalCard = null!;
    private UnlockBanner _unlockBanner = null!;
    private Button _unlocksButton = null!;
    private CodeWindow _codeWindow = null!;
    private UnlockPanel _unlockPanel = null!;
    private HelpPanel _helpPanel = null!;
    private HelpBook _help = null!;
    private HintTracker _hints = null!;
    private MenuButton _menuButton = null!;
    private ConfirmationDialog _newGameDialog = null!;

    private RunSession? _session;
    private bool _playing;
    private bool _wasRunning;
    private double _tickBudget;
    private ScriptLanguage _language = ScriptLanguage.Python;

    /// <inheritdoc/>
    public override void _Ready()
    {
        // The root window passes the theme on to every control and every window, dialogs included.
        GetTree().Root.Theme = GameTheme.Build();
        // The version lives only in Directory.Build.props; the title shows it so a tester can see which build runs.
        Version version = typeof(Main).Assembly.GetName().Version ?? throw new InvalidOperationException("the game assembly has no version");
        string versionText = $"Coding Game {version.ToString(3)}";
        GetWindow().Title = versionText;
        _rules = GameRules.Load(DataFolder);
        _codeWindow = new CodeWindow();
        AddChild(_codeWindow);
        SavedGame saved = LoadSavedGame();
        _progress = saved.Progress;
        _language = saved.Language;
        _scripts = saved.Scripts;
        _api = new DroneApi(_rules, _progress.Field);
        _help = HelpBook.Load(DataFolder, _api);
        _hints = new HintTracker(_rules);

        _codeWindow.Editor.Text = _scripts.Text(_scripts.Selected, _language);
        _codeWindow.ShowScripts(_scripts.Names, _scripts.Selected);
        _codeWindow.Editor.UseLanguage(_language, _api);
        _codeWindow.SelectLanguage(_language);
        _codeWindow.RunPressed += OnRun;
        _codeWindow.StepPressed += OnStep;
        _codeWindow.StopPressed += OnStop;
        _codeWindow.LanguageSelected += OnLanguageSelected;
        _codeWindow.ScriptSelected += OnScriptSelected;
        _codeWindow.ScriptAddPressed += OnScriptAddPressed;
        _codeWindow.ScriptRemovePressed += OnScriptRemovePressed;
        _codeWindow.ScriptRenameRequested += OnScriptRenameRequested;
        Vector2 viewport = GetViewportRect().Size;
        _codeWindow.Position = new Vector2I((int)(viewport.X * 0.55f), 80);
        _codeWindow.Size = new Vector2I((int)(viewport.X * 0.42f), (int)(viewport.Y * 0.8f));

        _fieldView = new FieldView3D();
        _fieldView.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_fieldView);

        _resourceBar = new ResourceBar { Position = new Vector2(16, 12) };
        AddChild(_resourceBar);
        _goalBar = new GoalBar { Position = new Vector2(16, 56) };
        AddChild(_goalBar);
        _goalCard = new GoalCard { Position = new Vector2(16, 112) };
        AddChild(_goalCard);

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

        _unlockBanner = new UnlockBanner();
        // Centred over the field, which sits left of the script window, not over the whole screen.
        _unlockBanner.AnchorLeft = FieldView3D.FieldCentreScreenFraction;
        _unlockBanner.AnchorRight = FieldView3D.FieldCentreScreenFraction;
        _unlockBanner.GrowHorizontal = GrowDirection.Both;
        _unlockBanner.OffsetTop = 72;
        _unlockBanner.ShowMePressed += () =>
        {
            helpWindow.Show();
            helpWindow.GrabFocus();
        };
        AddChild(_unlockBanner);

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
        _unlocksButton = new() { Text = "Unlocks" };
        ApplyRoundStyle(_unlocksButton);
        _unlocksButton.Pressed += () => ToggleWindow(unlocksWindow);
        Button helpButton = new() { Text = "Help" };
        ApplyRoundStyle(helpButton);
        helpButton.Pressed += () => ToggleWindow(helpWindow);
        // A menu button is flat by default, which hides the round background.
        _menuButton = new MenuButton { Text = "Menu", Flat = false };
        ApplyRoundStyle(_menuButton);
        PopupMenu menu = _menuButton.GetPopup();
        menu.AddItem("New game");

        // The editor's embedded game view hides the window title, so the menu shows the version too.
        menu.AddSeparator(versionText);
        menu.IdPressed += _ => _newGameDialog.PopupCentered();

        HBoxContainer roundButtons = new() { GrowHorizontal = GrowDirection.Begin };
        roundButtons.SetAnchorsPreset(LayoutPreset.TopRight);
        roundButtons.OffsetTop = 12;
        roundButtons.OffsetRight = -16;
        roundButtons.AddThemeConstantOverride("separation", 8);
        roundButtons.AddChild(codeButton);
        roundButtons.AddChild(_unlocksButton);
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
        if (_wasRunning && !running && _session is { } endedRun)
        {
            EndRun(endedRun);
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

    private static void ApplyRoundStyle(Button button)
    {
        button.CustomMinimumSize = new Vector2(48, 48);
        button.AddThemeStyleboxOverride("normal", RoundStyle(Palette.UiAccent));
        button.AddThemeStyleboxOverride("hover", RoundStyle(Palette.UiAccent.Lightened(0.12f)));
        button.AddThemeStyleboxOverride("pressed", RoundStyle(Palette.UiAccent.Darkened(0.15f)));
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
            return new SavedGame(new GameProgress(_rules), ScriptLanguage.Python, new ScriptBook(_rules));
        }
    }

    private void Save()
    {
        _scripts.SetText(_scripts.Selected, _language, _codeWindow.Editor.Text);
        SaveFile.Save(SavePath, new SavedGame(_progress, _language, _scripts));
    }

    private RunSession SessionForRun()
    {
        if (_session is null || _session.Status != StepStatus.Running)
        {
            _session = RunSession.Start(_codeWindow.Editor.Text, _language, _progress);
            _tickBudget = 0;
            Save();
            if (_session.Status != StepStatus.Running)
            {
                EndRun(_session);
            }
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

        // A run that failed when it started was ended in SessionForRun already.
        if (session.Status == StepStatus.Running)
        {
            session.StepLine();

            // A step can start and end a run within one frame, which the run check in _Process never sees.
            if (session.Status != StepStatus.Running)
            {
                // Clears the flag so _Process does not end this run a second time.
                _wasRunning = false;
                EndRun(session);
            }
        }
    }

    private void OnStop()
    {
        if (_session is { Status: StepStatus.Running } stopped)
        {
            EndRun(stopped);
        }

        _session = null;
        _playing = false;
    }

    private void EndRun(RunSession session)
    {
        _progress.CheckGoals(session);
        _codeWindow.ShowHint(_hints.RecordRunEnd(session));
        Save();
    }

    private void OnBought(Unlock unlock)
    {
        _unlockBanner.ShowUnlock(unlock);
        RefreshHelp();
        Save();
    }

    private void OnLanguageSelected(ScriptLanguage next)
    {
        _scripts.SetText(_scripts.Selected, _language, _codeWindow.Editor.Text);
        _language = next;
        _codeWindow.Editor.Text = _scripts.Text(_scripts.Selected, next);
        _codeWindow.Editor.UseLanguage(_language, _api);
        RefreshHelp();
    }

    private void OnScriptSelected(string name)
    {
        _scripts.SetText(_scripts.Selected, _language, _codeWindow.Editor.Text);
        _scripts.Select(name);
        ShowSelectedScript();
    }

    private void OnScriptAddPressed()
    {
        _scripts.SetText(_scripts.Selected, _language, _codeWindow.Editor.Text);
        _scripts.Add();
        ShowSelectedScript();
    }

    private void OnScriptRemovePressed(string name)
    {
        _scripts.SetText(_scripts.Selected, _language, _codeWindow.Editor.Text);
        _scripts.Remove(name);
        ShowSelectedScript();
    }

    private void OnScriptRenameRequested(string name, string newName)
    {
        try
        {
            _scripts.Rename(name, newName);
            _codeWindow.ShowNotice("");
        }
        catch (ArgumentException error)
        {
            // A name the player typed is input at the edge; the book's message tells them what to change.
            _codeWindow.ShowNotice($"Name not changed: {error.Message}.");
        }

        _codeWindow.ShowScripts(_scripts.Names, _scripts.Selected);
        Save();
    }

    // Shows the selected script in the editor and the tabs. An old run belonged to the previous script, so it is dropped.
    private void ShowSelectedScript()
    {
        _codeWindow.Editor.Text = _scripts.Text(_scripts.Selected, _language);
        _codeWindow.ShowScripts(_scripts.Names, _scripts.Selected);
        _session = null;
        _playing = false;
        _codeWindow.ShowNotice("");
        Save();
    }

    private void OnNewGameConfirmed()
    {
        _progress.Reset();
        _hints = new HintTracker(_rules);
        _scripts = new ScriptBook(_rules);
        _language = ScriptLanguage.Python;
        _codeWindow.SelectLanguage(_language);
        _codeWindow.Editor.Text = _scripts.Text(_scripts.Selected, _language);
        _codeWindow.ShowScripts(_scripts.Names, _scripts.Selected);
        _codeWindow.Editor.UseLanguage(_language, _api);
        _session = null;
        _playing = false;
        _tickBudget = 0;
        _codeWindow.ShowNotice("");
        _codeWindow.ShowHint(null);
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
        _goalBar.ShowNextGoal(_progress);
        _goalCard.ShowGoal(_progress);
        bool canBuyAny = _progress.Rules.Unlocks.Any(_progress.CanBuy);
        // A slow pulse draws the eye to Unlocks without blinking.
        _unlocksButton.Modulate = canBuyAny ? Colors.White.Lerp(Palette.UnlockGlow, 0.5f + 0.5f * Mathf.Sin(Time.GetTicksMsec() / 300f)) : Colors.White;
        _codeWindow.Refresh(session);

        bool running = session?.Status == StepStatus.Running;
        _menuButton.GetPopup().SetItemDisabled(0, running);
        _unlockPanel.Refresh(running);
    }
}
