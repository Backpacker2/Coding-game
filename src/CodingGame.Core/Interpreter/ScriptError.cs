using CodingGame.Core.Rules;

namespace CodingGame.Core.Interpreter;

/// <summary>
/// The programming language a script is written in. The error messages follow its conventions.
/// </summary>
public enum ScriptLanguage
{
    /// <summary>Python.</summary>
    Python,

    /// <summary>JavaScript.</summary>
    JavaScript,
}

/// <summary>
/// The kind of a script error. Each kind has one message per language, see <see cref="ScriptError.ToMessage"/>.
/// </summary>
public enum ScriptErrorKind
{
    /// <summary>A name that is neither a constant nor a command.</summary>
    UndefinedName,

    /// <summary>A value of the wrong kind, a wrong number of arguments or a wrong condition.</summary>
    WrongArgument,

    /// <summary>A feature that the player has not unlocked yet. The unlock that gives it is named in the message.</summary>
    NotUnlocked,

    /// <summary>Syntax the game does not have: no unlock gives it.</summary>
    NotInGame,

    /// <summary>A command that broke a game rule, such as planting a crop without its seed items.</summary>
    CommandFailed,

    /// <summary>A player function that calls itself too deeply.</summary>
    TooDeep,

    /// <summary>A script that is not valid in the language, for example a missing colon.</summary>
    SyntaxError,

    /// <summary>Python only: a block that is indented wrongly.</summary>
    IndentationError,
}

/// <summary>
/// An error in a player's script, with the line it happened on.
/// </summary>
/// <param name="Kind">The kind of error.</param>
/// <param name="Line">The line in the script, counted from 1.</param>
/// <param name="Detail">The name or the reason the message is about. For <see cref="ScriptErrorKind.UndefinedName"/> and <see cref="ScriptErrorKind.TooDeep"/> this is the bare name.</param>
/// <param name="Unlock">The unlock that gives the feature, with its title and price. Required for <see cref="ScriptErrorKind.NotUnlocked"/>, null for every other kind.</param>
public sealed record ScriptError(ScriptErrorKind Kind, int Line, string Detail, Unlock? Unlock = null)
{
    /// <summary>
    /// Writes the message the player sees, in the conventions of the script's language.
    /// </summary>
    /// <param name="language">The language of the script.</param>
    /// <returns>The message, starting with the error name of the language and the line number.</returns>
    /// <exception cref="InvalidOperationException">The kind is <see cref="ScriptErrorKind.NotUnlocked"/> and the error has no unlock.</exception>
    public string ToMessage(ScriptLanguage language)
    {
        if (Kind == ScriptErrorKind.NotUnlocked)
        {
            Unlock unlock = Unlock ?? throw new InvalidOperationException("a NotUnlocked error carries its unlock");
            return $"Error on line {Line}: '{Detail}' is not unlocked yet. Buy \"{unlock.Title}\" in Unlocks for {unlock.Price.Count} {unlock.Price.Item}.";
        }

        return (Kind, language) switch
        {
            (ScriptErrorKind.UndefinedName, ScriptLanguage.Python) => $"NameError on line {Line}: name '{Detail}' is not defined",
            (ScriptErrorKind.UndefinedName, ScriptLanguage.JavaScript) => $"ReferenceError on line {Line}: {Detail} is not defined",
            (ScriptErrorKind.WrongArgument, ScriptLanguage.Python) => $"TypeError on line {Line}: {Detail}",
            (ScriptErrorKind.WrongArgument, ScriptLanguage.JavaScript) => $"TypeError on line {Line}: {Detail}",
            (ScriptErrorKind.NotInGame, _) => $"Error on line {Line}: '{Detail}' is not part of this game yet",
            (ScriptErrorKind.CommandFailed, ScriptLanguage.Python) => $"RuntimeError on line {Line}: {Detail}",
            (ScriptErrorKind.CommandFailed, ScriptLanguage.JavaScript) => $"Error on line {Line}: {Detail}",
            (ScriptErrorKind.TooDeep, ScriptLanguage.Python) => $"RecursionError on line {Line}: maximum recursion depth exceeded in {Detail}()",
            (ScriptErrorKind.TooDeep, ScriptLanguage.JavaScript) => $"RangeError on line {Line}: Maximum call stack size exceeded in {Detail}",
            (ScriptErrorKind.SyntaxError, ScriptLanguage.Python) => $"SyntaxError on line {Line}: {Detail}",
            (ScriptErrorKind.SyntaxError, ScriptLanguage.JavaScript) => $"SyntaxError on line {Line}: {Detail}",
            (ScriptErrorKind.IndentationError, ScriptLanguage.Python) => $"IndentationError on line {Line}: {Detail}",
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, "unknown script error kind or language"),
        };
    }
}
