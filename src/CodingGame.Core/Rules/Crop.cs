namespace CodingGame.Core.Rules;

/// <summary>
/// What a tile is made of.
/// </summary>
public enum Ground
{
    /// <summary>Untilled grass.</summary>
    Grass,

    /// <summary>Tilled soil that can be planted.</summary>
    Soil,
}

/// <summary>
/// The items paid to plant a crop.
/// </summary>
/// <param name="Item">The name of the item that is paid.</param>
/// <param name="Count">How many of the item are paid.</param>
public sealed record SeedCost(string Item, int Count);

/// <summary>
/// A kind of crop and its rules.
/// </summary>
/// <param name="Name">The name of the crop.</param>
/// <param name="Ground">The ground the crop grows on.</param>
/// <param name="GrowTimeTicks">How many ticks the crop takes to ripen.</param>
/// <param name="YieldItems">How many items one harvest gives.</param>
/// <param name="Seed">The items paid to plant the crop. Null means the crop grows wild and cannot be planted.</param>
/// <param name="Regrows">True means the crop grows back after a harvest. A crop that does not regrow is destroyed when harvested before it is ripe.</param>
public sealed record Crop(
    string Name,
    Ground Ground,
    int GrowTimeTicks,
    int YieldItems,
    SeedCost? Seed,
    bool Regrows);
