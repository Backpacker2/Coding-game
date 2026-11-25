namespace CodingGame.Core.Interpreter;

/// <summary>
/// The result of a front end: the program, or the first error in the script. Exactly one of the two is not null.
/// </summary>
/// <param name="Program">The parsed program, or null when the script has an error.</param>
/// <param name="Error">The first error in the script, or null when the script parsed.</param>
public readonly record struct ParseResult(ScriptProgram? Program, ScriptError? Error)
{
    /// <summary>
    /// Creates the result of a script that parsed.
    /// </summary>
    /// <param name="program">The parsed program.</param>
    /// <returns>A result that holds the program and no error.</returns>
    public static ParseResult Success(ScriptProgram program)
    {
        return new ParseResult(program, null);
    }

    /// <summary>
    /// Creates the result of a script that has an error.
    /// </summary>
    /// <param name="error">The first error in the script.</param>
    /// <returns>A result that holds the error and no program.</returns>
    public static ParseResult Failure(ScriptError error)
    {
        return new ParseResult(null, error);
    }
}
