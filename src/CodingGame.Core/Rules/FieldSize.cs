namespace CodingGame.Core.Rules;

/// <summary>
/// The size of the field the game starts with, in tiles.
/// </summary>
/// <param name="Width">The number of columns.</param>
/// <param name="Height">The number of rows.</param>
public sealed record FieldSize(int Width, int Height);
