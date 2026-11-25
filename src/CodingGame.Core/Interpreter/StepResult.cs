namespace CodingGame.Core.Interpreter;

/// <summary>
/// The state of an evaluator after a step.
/// </summary>
public enum StepStatus
{
    /// <summary>The script is not finished and can be stepped again.</summary>
    Running,

    /// <summary>The script ran to its end.</summary>
    Finished,

    /// <summary>The script hit an error and stopped.</summary>
    Failed,
}

/// <summary>
/// The result of one step of the evaluator.
/// </summary>
/// <param name="Status">Whether the script is still running, finished or failed.</param>
/// <param name="Error">The error when the status is <see cref="StepStatus.Failed"/>, otherwise null.</param>
public readonly record struct StepResult(StepStatus Status, ScriptError? Error)
{
    /// <summary>
    /// Gets the result of a step after which the script is still running.
    /// </summary>
    public static StepResult Running => new(StepStatus.Running, null);

    /// <summary>
    /// Gets the result of a step after which the script has run to its end.
    /// </summary>
    public static StepResult Finished => new(StepStatus.Finished, null);

    /// <summary>
    /// Creates the result of a step that stopped on a script error.
    /// </summary>
    /// <param name="error">The error that stopped the script.</param>
    /// <returns>A result with status <see cref="StepStatus.Failed"/>.</returns>
    public static StepResult Failed(ScriptError error)
    {
        return new StepResult(StepStatus.Failed, error);
    }
}
