using CodingGame.Core.Interpreter;
using CodingGame.Core.Session;
using Godot;
using ScriptLanguage = CodingGame.Core.Interpreter.ScriptLanguage;

namespace CodingGame.Godot;

/// <summary>
/// The floating script window: a tab per script with + to add one, the language picker, Run, Step and Stop,
/// the speed picker, the tick count, the script editor and the Messages box. Closing it hides the window; the
/// Code button shows it again.
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
    private Button _hintButton = null!;
    private Label _hintLabel = null!;
    private TabBar _scriptTabs = null!;
    private Button _addScriptButton = null!;
    private ConfirmationDialog _renameDialog = null!;
    private LineEdit _renameEdit = null!;
    private string _renamingName = "";
    private string _selectedName = "";
    private TabBar.CloseButtonDisplayPolicy _closePolicy;
    private bool _running;

    /// <summary>Raised when the player presses Run.</summary>
    public event Action? RunPressed;

    /// <summary>Raised when the player presses Step.</summary>
    public event Action? StepPressed;

    /// <summary>Raised when the player presses Stop.</summary>
    public event Action? StopPressed;

    /// <summary>Raised when the player picks another language in the language picker.</summary>
    public event Action<ScriptLanguage>? LanguageSelected;

    /// <summary>Raised when the player picks another script tab, with that script's name.</summary>
    public event Action<string>? ScriptSelected;

    /// <summary>Raised when the player presses + to add a script.</summary>
    public event Action? ScriptAddPressed;

    /// <summary>Raised when the player closes a script tab, with that script's name.</summary>
    public event Action<string>? ScriptRemovePressed;

    /// <summary>Raised when the player confirms a new name for a script: the current name, then the new name as typed.</summary>
    public event Action<string, string>? ScriptRenameRequested;

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

    /// <summary>
    /// Shows one tab per script with the selected one active, without raising <see cref="ScriptSelected"/>. A tab can
    /// be closed only while there is more than one script, because a game always keeps one.
    /// </summary>
    /// <param name="names">The names of the scripts, in tab order.</param>
    /// <param name="selected">The name of the script to show as active.</param>
    public void ShowScripts(IReadOnlyList<string> names, string selected)
    {
        _scriptTabs.SetBlockSignals(true);
        _selectedName = selected;
        _scriptTabs.ClearTabs();
        foreach (string name in names)
        {
            _scriptTabs.AddTab(name);
        }

        _scriptTabs.CurrentTab = names.ToList().IndexOf(selected);
        _closePolicy = names.Count > 1 ? TabBar.CloseButtonDisplayPolicy.ShowActiveOnly : TabBar.CloseButtonDisplayPolicy.ShowNever;
        _scriptTabs.TabCloseDisplayPolicy = _closePolicy;
        _scriptTabs.SetBlockSignals(false);
    }

    /// <summary>Shows a red notice above the Messages box. An empty text clears it.</summary>
    /// <param name="text">The notice to show.</param>
    public void ShowNotice(string text)
    {
        _noticeLabel.Text = text;
    }

    /// <summary>
    /// Shows a hint under the Messages box, folded behind a Show hint button so a player who wants to solve it alone
    /// is not told the answer. Null removes the hint and its button.
    /// </summary>
    /// <param name="hint">The hint text, or null for none.</param>
    public void ShowHint(string? hint)
    {
        _hintLabel.Text = hint is null ? "" : $"Hint: {hint}";
        _hintButton.Visible = hint is not null;
        _hintButton.ButtonPressed = false;
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
        // The tabs lock during a run, so the running script is always the selected tab, which gets a mark.
        _running = running;
        _addScriptButton.Disabled = running;
        for (int index = 0; index < _scriptTabs.TabCount; index++)
        {
            _scriptTabs.SetTabDisabled(index, running && index != _scriptTabs.CurrentTab);
        }

        _scriptTabs.SetTabTitle(_scriptTabs.CurrentTab, running ? $"▶ {_selectedName}" : _selectedName);
        _scriptTabs.TabCloseDisplayPolicy = running ? TabBar.CloseButtonDisplayPolicy.ShowNever : _closePolicy;

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

        _scriptTabs = new TabBar { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClipTabs = true };
        _scriptTabs.TooltipText = "Right-click a tab to rename it";
        _scriptTabs.TabChanged += index => ScriptSelected?.Invoke(_scriptTabs.GetTabTitle((int)index));
        _scriptTabs.TabClosePressed += index => ScriptRemovePressed?.Invoke(_scriptTabs.GetTabTitle((int)index));
        _scriptTabs.TabRmbClicked += index =>
        {
            // A running script cannot be renamed, and its tab title carries the run mark.
            if (_running)
            {
                return;
            }

            _renamingName = _scriptTabs.GetTabTitle((int)index);
            _renameEdit.Text = _renamingName;
            _renameDialog.PopupCentered(new Vector2I(320, 100));
            _renameEdit.GrabFocus();
            _renameEdit.SelectAll();
        };
        _addScriptButton = new Button { Text = "+", TooltipText = "Add a script" };
        _addScriptButton.Pressed += () => ScriptAddPressed?.Invoke();
        HBoxContainer scriptRow = new();
        scriptRow.AddChild(_scriptTabs);
        scriptRow.AddChild(_addScriptButton);

        _renameEdit = new LineEdit { MaxLength = ScriptBook.MaxNameLength };
        _renameDialog = new ConfirmationDialog { Title = "Rename script", OkButtonText = "Rename" };
        _renameDialog.AddChild(_renameEdit);
        AddChild(_renameDialog);
        _renameDialog.Confirmed += () => ScriptRenameRequested?.Invoke(_renamingName, _renameEdit.Text);
        _renameEdit.TextSubmitted += _ =>
        {
            _renameDialog.Hide();
            ScriptRenameRequested?.Invoke(_renamingName, _renameEdit.Text);
        };

        _languagePicker = new OptionButton();
        _languagePicker.AddItem("Python");
        _languagePicker.AddItem("JavaScript");
        _languagePicker.ItemSelected += index => LanguageSelected?.Invoke((ScriptLanguage)index);

        Button runButton = new() { Text = "▶ Run" };
        GameTheme.ColorButton(runButton, Palette.UiAccent);
        runButton.Pressed += () => RunPressed?.Invoke();
        Button stepButton = new() { Text = "Step" };
        stepButton.Pressed += () => StepPressed?.Invoke();
        _stopButton = new Button { Text = "■ Stop" };
        GameTheme.ColorButton(_stopButton, Palette.UiDanger);
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
        errorPanel.AddThemeStyleboxOverride("panel", GameTheme.Box(Palette.UiEditor, 6));
        errorPanel.AddChild(_errorLabel);
        _hintLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, Visible = false };
        _hintLabel.AddThemeColorOverride("font_color", Palette.HintText);
        _hintButton = new Button { Text = "Show hint", ToggleMode = true, Visible = false, SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin };
        _hintButton.Toggled += pressed => _hintLabel.Visible = pressed;

        VBoxContainer column = new();
        column.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        column.AddChild(scriptRow);
        column.AddChild(toolbar);
        column.AddChild(Editor);
        column.AddChild(_noticeLabel);
        column.AddChild(new Label { Text = "Messages" });
        column.AddChild(errorPanel);
        column.AddChild(_hintButton);
        column.AddChild(_hintLabel);
        AddChild(column);
    }
}
