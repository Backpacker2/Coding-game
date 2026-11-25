namespace CodingGame.Core.Interpreter;

/// <summary>
/// A script as a front end hands it to the evaluator.
/// </summary>
/// <param name="Body">The top-level statements, run in order.</param>
/// <param name="Language">The language the script is written in, used for error messages.</param>
public sealed record ScriptProgram(IReadOnlyList<Statement> Body, ScriptLanguage Language);
