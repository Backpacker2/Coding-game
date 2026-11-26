using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using Godot;

namespace CodingGame.Godot;

/// <summary>
/// A small card under the goal bar with the current first-steps goal in one sentence and a ? button that shows its hint. When goals get done, it shows a tick for each of them for a moment before the next goal. With every goal done it hides. It only reads the progress.
/// </summary>
public partial class GoalCard : PanelContainer
{
    // Long enough to read the tick, short enough not to hold back the next goal.
    private const ulong DoneMilliseconds = 1500;

    private readonly Label _goalLabel = new();
    private readonly Label _hintLabel = new();
    private readonly Button _hintButton = new() { Text = "?", TooltipText = "Show a hint", ToggleMode = true };
    private HashSet<string>? _doneGoalIds;
    private ulong _doneUntilMsec;

    /// <summary>
    /// Builds the card, with the hint hidden until the ? button is pressed. The card starts hidden.
    /// </summary>
    public GoalCard()
    {
        StyleBoxFlat background = new() { BgColor = Palette.GoalCardBackground };
        background.SetCornerRadiusAll(6);
        background.SetContentMarginAll(8);
        AddThemeStyleboxOverride("panel", background);

        _goalLabel.AddThemeFontSizeOverride("font_size", 15);
        _goalLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _goalLabel.CustomMinimumSize = new Vector2(240, 0);
        _goalLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        _hintButton.Toggled += pressed => _hintLabel.Visible = pressed;

        _hintLabel.AddThemeFontSizeOverride("font_size", 14);
        _hintLabel.AddThemeColorOverride("font_color", Palette.MutedText);
        _hintLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _hintLabel.Visible = false;

        HBoxContainer row = new();
        row.AddChild(_goalLabel);
        row.AddChild(_hintButton);
        VBoxContainer content = new();
        content.AddChild(row);
        content.AddChild(_hintLabel);
        AddChild(content);

        Visible = false;
    }

    /// <summary>
    /// Shows the current first-steps goal and its hint, or the tick for a goal that was just done. Called every frame; it is cheap.
    /// </summary>
    /// <param name="progress">The progress to read the current goal from. It is not changed.</param>
    public void ShowGoal(GameProgress progress)
    {
        // Comparing with the goals done last frame finds every way goals get done (a run, a purchase, several at
        // once) in one place. The first frame has nothing to compare with, so it only records them.
        HashSet<string> doneIds = progress.Rules.Goals.Where(progress.IsDone).Select(goal => goal.Id).ToHashSet();
        List<Goal> newlyDone = _doneGoalIds is null
            ? []
            : progress.Rules.Goals.Where(goal => doneIds.Contains(goal.Id) && !_doneGoalIds.Contains(goal.Id)).ToList();
        _doneGoalIds = doneIds;
        if (newlyDone.Count > 0)
        {
            ShowDone(newlyDone);
        }

        Goal? current = progress.CurrentGoal;
        if (Time.GetTicksMsec() < _doneUntilMsec)
        {
            return;
        }

        if (current is null)
        {
            Visible = false;
            return;
        }

        _goalLabel.Text = current.Text;
        _goalLabel.RemoveThemeColorOverride("font_color");
        _hintLabel.Text = current.Hint;
        _hintButton.Visible = true;
        Visible = true;
    }

    private void ShowDone(List<Goal> goals)
    {
        _doneUntilMsec = Time.GetTicksMsec() + DoneMilliseconds;
        _goalLabel.Text = string.Join("\n", goals.Select(goal => $"✓ {goal.Text}"));
        _goalLabel.AddThemeColorOverride("font_color", Palette.GoalDone);
        _hintButton.Visible = false;
        _hintButton.ButtonPressed = false;
        Visible = true;
    }
}
