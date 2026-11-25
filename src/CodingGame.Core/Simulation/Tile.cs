using CodingGame.Core.Rules;

namespace CodingGame.Core.Simulation;

/// <summary>
/// A tile on the field. X is the column from west to east, Y is the row. North decreases Y.
/// </summary>
/// <param name="X">The column, counted from the west edge.</param>
/// <param name="Y">The row, counted from the north edge.</param>
public readonly record struct TilePosition(int X, int Y);

/// <summary>
/// What one tile of the field holds.
/// </summary>
/// <param name="Ground">What the tile is made of.</param>
/// <param name="Crop">The crop growing on the tile, or null when the tile has no crop.</param>
/// <param name="CropAgeTicks">How many ticks the crop has grown so far.</param>
public readonly record struct Tile(Ground Ground, Crop? Crop, int CropAgeTicks);
