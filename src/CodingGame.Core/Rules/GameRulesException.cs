namespace CodingGame.Core.Rules;

/// <summary>
/// Thrown when a file in data/ is missing or invalid.
/// </summary>
public sealed class GameRulesException : Exception
{
    /// <summary>
    /// Creates an exception that names the broken file and the problem.
    /// </summary>
    /// <param name="fileName">The name of the file in data/ that is missing or invalid.</param>
    /// <param name="problem">A description of what is wrong with the file.</param>
    public GameRulesException(string fileName, string problem)
        : base($"{fileName}: {problem}")
    {
    }
}
