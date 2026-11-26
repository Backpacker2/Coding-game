namespace CodingGame.Core.Session;

/// <summary>
/// Thrown when a save file cannot be read: it is broken, from another game version, or breaks a rule of the game.
/// </summary>
public sealed class SaveException : Exception
{
    /// <summary>
    /// Creates an exception that names the save file and the problem.
    /// </summary>
    /// <param name="fileName">The name of the save file, for example save.json.</param>
    /// <param name="problem">A description of what is wrong with the file.</param>
    public SaveException(string fileName, string problem)
        : base($"{fileName}: {problem}")
    {
    }
}
