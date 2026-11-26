using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.JavaScript;
using CodingGame.Core.Interpreter.Python;
using CodingGame.Core.Simulation;

namespace CodingGame.Core.Session;

/// <summary>
/// A script run in progress on the field of the player's progress. It is started from the script text,
/// the language and the progress, then advanced one tick or one statement at a time. A script that does not
/// parse, or that uses a feature the player has not unlocked, is a session that has failed from the start.
/// </summary>
public sealed class RunSession
{
    /// <summary>
    /// The most ticks one call of <see cref="StepLine"/> may run. A loop that never starts another
    /// statement, such as <c>while (ripe()) {}</c>, would otherwise never return.
    /// </summary>
    public const int MaxTicksPerLineStep = 10_000;

    private readonly ScriptLanguage _language;
    private readonly Evaluator? _evaluator;
    private readonly bool[] _visitedTiles;
    private int _unvisitedTileCount;

    private RunSession(ScriptLanguage language, Field field, Evaluator? evaluator, StepStatus status, ScriptError? error)
    {
        _language = language;
        Field = field;
        _evaluator = evaluator;
        Status = status;
        Error = error;
        _visitedTiles = new bool[field.Width * field.Height];
        _unvisitedTileCount = _visitedTiles.Length;
        MarkDroneTileVisited();
    }

    /// <summary>
    /// Gets the field the script runs on. It is changed only by the steps of this session.
    /// </summary>
    public Field Field { get; }

    /// <summary>
    /// Gets whether the drone has stood on every tile of the field during this run. The tile the drone starts on counts.
    /// </summary>
    public bool VisitedAllTiles => _unvisitedTileCount == 0;

    /// <summary>
    /// Gets whether the script is still running, has finished, or has failed on a parse or runtime error.
    /// </summary>
    public StepStatus Status { get; private set; }

    /// <summary>
    /// Gets the error that stopped the script, or null when there is none.
    /// </summary>
    public ScriptError? Error { get; private set; }

    /// <summary>
    /// Gets the message for the error, in the language of the script, or null when there is no error.
    /// </summary>
    public string? ErrorMessage => Error?.ToMessage(_language);

    /// <summary>
    /// Gets the line of the statement that was started last. It is 0 before the first step and after a parse error.
    /// </summary>
    public int CurrentLine => _evaluator?.CurrentLine ?? 0;

    /// <summary>
    /// Gets the number of ticks the script has used. It is 0 after a parse error.
    /// </summary>
    public int TicksUsed => _evaluator?.TicksUsed ?? 0;

    /// <summary>
    /// Parses a script and checks it against the player's unlocks. Creates a session that has not run any
    /// tick yet, or a session that has failed if the script does not parse or uses a locked feature.
    /// The session runs on the field of the progress, so the crops and the drone keep what the run changed.
    /// </summary>
    /// <param name="source">The script text.</param>
    /// <param name="language">The language the script is written in.</param>
    /// <param name="progress">The player's progress: the unlocks decide what may be used, and the field and items come from it.</param>
    /// <returns>The new session.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The language is not a known language.</exception>
    public static RunSession Start(string source, ScriptLanguage language, GameProgress progress)
    {
        ParseResult parsed = language switch
        {
            ScriptLanguage.Python => PythonFrontEnd.Parse(source),
            ScriptLanguage.JavaScript => JavaScriptFrontEnd.Parse(source),
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, "unknown script language"),
        };
        Field field = progress.Field;
        if (parsed.Error is { } error)
        {
            return new RunSession(language, field, null, StepStatus.Failed, error);
        }

        ScriptProgram program = parsed.Program ?? throw new InvalidOperationException("a parse result has a program or an error");
        if (UnlockCheck.FindLocked(program, progress.LockedFeatures) is { } lockedError)
        {
            return new RunSession(language, field, null, StepStatus.Failed, lockedError);
        }

        var evaluator = new Evaluator(program, new DroneApi(progress.Rules, field));
        return new RunSession(language, field, evaluator, StepStatus.Running, null);
    }

    /// <summary>
    /// Runs one tick of the script. Does nothing once the script has finished or failed.
    /// </summary>
    public void StepTick()
    {
        if (_evaluator is null || Status != StepStatus.Running)
        {
            return;
        }

        StepResult result = _evaluator.Step();
        Status = result.Status;
        Error = result.Error;
        MarkDroneTileVisited();
    }

    /// <summary>
    /// Runs ticks until the script starts a different statement, finishes or fails. Stops early after
    /// <see cref="MaxTicksPerLineStep"/> ticks, so the session is still running when a loop never
    /// starts another statement. Does nothing once the script has finished or failed.
    /// </summary>
    public void StepLine()
    {
        int startLine = CurrentLine;
        for (int ticks = 0; ticks < MaxTicksPerLineStep && Status == StepStatus.Running && CurrentLine == startLine; ticks++)
        {
            StepTick();
        }
    }

    // A move is always followed by a tick, so checking the drone after each tick sees every tile it lands on.
    private void MarkDroneTileVisited()
    {
        int index = Field.DronePosition.Y * Field.Width + Field.DronePosition.X;
        if (!_visitedTiles[index])
        {
            _visitedTiles[index] = true;
            _unvisitedTileCount--;
        }
    }
}
