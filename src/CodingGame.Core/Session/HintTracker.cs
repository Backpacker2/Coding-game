using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;

namespace CodingGame.Core.Session;

/// <summary>
/// Counts how many runs in a row end with the same script error, and gives the hint for that error once the player seems stuck.
/// The count lives in the play session only and is not saved, so a loaded game starts counting from zero.
/// </summary>
public sealed class HintTracker
{
    /// <summary>
    /// The number of runs in a row with the same error from which the hint is given.
    /// </summary>
    public const int RepeatsBeforeHint = 3;

    private readonly GameRules _rules;
    private ScriptError? _lastError;
    private int _repeats;

    /// <summary>
    /// Creates a tracker that has not counted any error yet.
    /// </summary>
    /// <param name="rules">The game rules, with the hints from hints.json.</param>
    public HintTracker(GameRules rules)
    {
        _rules = rules;
    }

    /// <summary>
    /// Records how a run ended and returns the hint to show for it. Two errors are the same when their kind and detail are equal, so a moved line still counts as the same error.
    /// A run without an error resets the count.
    /// </summary>
    /// <param name="endedRun">The run that has ended, or that failed when it started.</param>
    /// <returns>The hint text, or null when the run has no error, the error has not repeated <see cref="RepeatsBeforeHint"/> times in a row, or no hint fits the error.</returns>
    public string? RecordRunEnd(RunSession endedRun)
    {
        ScriptError? error = endedRun.Error;
        if (error is null)
        {
            _lastError = null;
            _repeats = 0;
            return null;
        }

        if (_lastError is not null && _lastError.Kind == error.Kind && _lastError.Detail == error.Detail)
        {
            _repeats++;
        }
        else
        {
            _repeats = 1;
        }

        _lastError = error;
        return _repeats >= RepeatsBeforeHint ? FindHint(error) : null;
    }

    private string? FindHint(ScriptError error)
    {
        return _rules.Hints
            .FirstOrDefault(hint => hint.Kind == error.Kind
                && (hint.DetailContains is null || error.Detail.Contains(hint.DetailContains, StringComparison.Ordinal)))
            ?.Text;
    }
}
