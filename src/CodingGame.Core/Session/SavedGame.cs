using CodingGame.Core.Interpreter;

namespace CodingGame.Core.Session;

/// <summary>
/// One of the player's scripts: its name and the text it has in each language.
/// </summary>
/// <param name="Name">The name the player gave the script, shown on its tab.</param>
/// <param name="Python">The Python text of the script.</param>
/// <param name="JavaScript">The JavaScript text of the script.</param>
public sealed record SavedScript(string Name, string Python, string JavaScript);

/// <summary>
/// Everything the game keeps between sessions: the progress, the selected language and the scripts.
/// </summary>
/// <param name="Progress">The items and the bought unlocks.</param>
/// <param name="Language">The language the player had selected.</param>
/// <param name="Scripts">The player's scripts, in tab order, with one selected.</param>
public sealed record SavedGame(GameProgress Progress, ScriptLanguage Language, ScriptBook Scripts);
