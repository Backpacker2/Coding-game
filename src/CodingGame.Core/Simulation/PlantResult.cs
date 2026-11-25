namespace CodingGame.Core.Simulation;

/// <summary>
/// The outcome of trying to plant a crop on the tile under the drone. A result other than
/// <see cref="Planted"/> leaves the field and the items unchanged.
/// </summary>
public enum PlantResult
{
    /// <summary>The crop was planted and the seed items were paid.</summary>
    Planted,

    /// <summary>The crop has no seed cost, so it grows wild and cannot be planted.</summary>
    NotPlantable,

    /// <summary>The tile under the drone already has a crop.</summary>
    TileOccupied,

    /// <summary>The tile is not made of the ground the crop grows on.</summary>
    WrongGround,

    /// <summary>The drone does not hold enough of the seed item.</summary>
    NotEnoughItems,
}
