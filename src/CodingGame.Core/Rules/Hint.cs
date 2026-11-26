using CodingGame.Core.Interpreter;

namespace CodingGame.Core.Rules;

/// <summary>
/// A tip that is shown after the same script error has happened several times in a row.
/// </summary>
/// <param name="Kind">The kind of error the hint is for.</param>
/// <param name="DetailContains">A text that the error detail must contain for the hint to apply, or null when any detail of that kind fits.</param>
/// <param name="Text">The tip the player reads, a language-neutral sentence.</param>
public sealed record Hint(ScriptErrorKind Kind, string? DetailContains, string Text);
