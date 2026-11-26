using CodingGame.Core.Interpreter;

namespace CodingGame.Core.Session;

/// <summary>
/// The script text the player has for each language. A null text means the script was never saved.
/// </summary>
/// <param name="Python">The Python script, or null when it was never saved.</param>
/// <param name="JavaScript">The JavaScript script, or null when it was never saved.</param>
public sealed record SavedScripts(string? Python, string? JavaScript);

/// <summary>
/// Everything the game keeps between sessions: the progress, the selected language and the scripts.
/// </summary>
/// <param name="Progress">The items and the bought unlocks.</param>
/// <param name="Language">The language the player had selected.</param>
/// <param name="Scripts">The script text of each language.</param>
public sealed record SavedGame(GameProgress Progress, ScriptLanguage Language, SavedScripts Scripts);
