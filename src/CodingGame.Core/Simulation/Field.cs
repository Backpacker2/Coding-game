using CodingGame.Core.Rules;

namespace CodingGame.Core.Simulation;

/// <summary>
/// The farm grid, the drone standing on it and the items the drone has harvested.
/// The field does not wrap for tile access, but the drone wraps around the edges when it moves.
/// </summary>
public sealed class Field
{
    private readonly Tile[,] _tiles;
    private readonly Dictionary<string, int> _items;

    /// <summary>
    /// Creates a field of grass where the wild crop is ripe on every tile. The drone starts at (0, 0).
    /// The field starts with no items.
    /// </summary>
    /// <param name="width">The number of columns. Must be at least 1.</param>
    /// <param name="height">The number of rows. Must be at least 1.</param>
    /// <param name="wildCrop">The crop that grows wild, ripe, on every tile at the start.</param>
    /// <exception cref="ArgumentOutOfRangeException">The width or the height is less than 1.</exception>
    public Field(int width, int height, Crop wildCrop)
        : this(width, height, wildCrop, [])
    {
    }

    /// <summary>
    /// Creates a field of grass where the wild crop is ripe on every tile. The drone starts at (0, 0).
    /// The field keeps its items in <paramref name="items"/>, so the caller sees every harvest and every seed payment.
    /// </summary>
    /// <param name="width">The number of columns. Must be at least 1.</param>
    /// <param name="height">The number of rows. Must be at least 1.</param>
    /// <param name="wildCrop">The crop that grows wild, ripe, on every tile at the start.</param>
    /// <param name="items">The item store the field reads and writes. Every count must be at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">The width or the height is less than 1.</exception>
    public Field(int width, int height, Crop wildCrop, Dictionary<string, int> items)
        : this(CreateWildTiles(width, height, wildCrop), new TilePosition(0, 0), items)
    {
    }

    /// <summary>
    /// Restores a field from saved tiles. The array is copied, so later changes to it do not change the field.
    /// The field keeps its items in <paramref name="items"/>.
    /// </summary>
    /// <param name="tiles">The tiles, indexed by column and row. Its width and height are the size of the field, and must be at least 1 x 1.</param>
    /// <param name="dronePosition">The tile the drone stands on. Must be on the field.</param>
    /// <param name="items">The item store the field reads and writes. Every count must be at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">The size of <paramref name="tiles"/> is less than 1 in either direction, or the drone is outside the field.</exception>
    public Field(Tile[,] tiles, TilePosition dronePosition, Dictionary<string, int> items)
    {
        int width = tiles.GetLength(0);
        int height = tiles.GetLength(1);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(dronePosition.X);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(dronePosition.X, width);
        ArgumentOutOfRangeException.ThrowIfNegative(dronePosition.Y);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(dronePosition.Y, height);
        Width = width;
        Height = height;
        DronePosition = dronePosition;
        _items = items;
        _tiles = (Tile[,])tiles.Clone();
    }

    /// <summary>
    /// Gets the number of columns.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the number of rows.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the tile the drone stands on.
    /// </summary>
    public TilePosition DronePosition { get; private set; }

    /// <summary>
    /// Gets the items the drone holds, keyed by item name. Every count is at least 1: an item the drone has none of is not listed.
    /// </summary>
    public IReadOnlyDictionary<string, int> Items => _items;

    private Tile DroneTile
    {
        get => _tiles[DronePosition.X, DronePosition.Y];
        set => _tiles[DronePosition.X, DronePosition.Y] = value;
    }

    /// <summary>
    /// Gets the tile at a position on the field.
    /// </summary>
    /// <param name="x">The column, from 0 to Width - 1.</param>
    /// <param name="y">The row, from 0 to Height - 1.</param>
    /// <returns>The tile at that position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The position is outside the field.</exception>
    public Tile GetTile(int x, int y)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(x);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Width);
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height);
        return _tiles[x, y];
    }

    /// <summary>
    /// Moves the drone one tile in a direction. At an edge the drone continues on the opposite edge.
    /// </summary>
    /// <param name="direction">The direction to move in.</param>
    public void Move(Direction direction)
    {
        DronePosition = direction switch
        {
            Direction.North => new TilePosition(DronePosition.X, WrapIndex(DronePosition.Y - 1, Height)),
            Direction.South => new TilePosition(DronePosition.X, WrapIndex(DronePosition.Y + 1, Height)),
            Direction.East => new TilePosition(WrapIndex(DronePosition.X + 1, Width), DronePosition.Y),
            Direction.West => new TilePosition(WrapIndex(DronePosition.X - 1, Width), DronePosition.Y),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "unknown direction"),
        };
    }

    /// <summary>
    /// Gets the ground under the drone.
    /// </summary>
    /// <returns>The ground of the tile the drone stands on.</returns>
    public Ground GetGround()
    {
        return DroneTile.Ground;
    }

    /// <summary>
    /// Checks whether the crop on the tile under the drone is ripe.
    /// </summary>
    /// <returns>True when the tile has a crop that has grown for its full grow time; false when the tile has no crop.</returns>
    public bool IsRipe()
    {
        Tile tile = DroneTile;
        return tile.Crop is not null && tile.CropAgeTicks >= tile.Crop.GrowTimeTicks;
    }

    /// <summary>
    /// Lets every crop on the field grow by a number of ticks. A crop stops growing once it is ripe.
    /// </summary>
    /// <param name="ticks">How many ticks pass. Must not be negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">The number of ticks is negative.</exception>
    public void Advance(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Tile tile = _tiles[x, y];
                if (tile.Crop is not null)
                {
                    int remainingTicks = tile.Crop.GrowTimeTicks - tile.CropAgeTicks;
                    int ageTicks = ticks >= remainingTicks ? tile.Crop.GrowTimeTicks : tile.CropAgeTicks + ticks;
                    _tiles[x, y] = tile with { CropAgeTicks = ageTicks };
                }
            }
        }
    }

    /// <summary>
    /// Harvests the crop on the tile under the drone. A ripe crop gives its yield to the items.
    /// A crop that regrows stays on the tile, otherwise the crop is removed, also when it was not ripe.
    /// An unripe crop that regrows is left unchanged.
    /// </summary>
    public void Harvest()
    {
        Tile tile = DroneTile;
        if (tile.Crop is null)
        {
            return;
        }

        bool ripe = IsRipe();
        if (ripe)
        {
            _items[tile.Crop.Name] = _items.GetValueOrDefault(tile.Crop.Name) + tile.Crop.YieldItems;
        }

        if (!tile.Crop.Regrows)
        {
            DroneTile = new Tile(tile.Ground, null, 0);
        }
        else if (ripe)
        {
            DroneTile = tile with { CropAgeTicks = 0 };
        }
    }

    /// <summary>
    /// Tills the tile under the drone. Grass becomes soil and soil becomes grass. Any crop on the tile is removed.
    /// </summary>
    public void Till()
    {
        Tile tile = DroneTile;
        Ground ground = tile.Ground == Ground.Grass ? Ground.Soil : Ground.Grass;
        DroneTile = new Tile(ground, null, 0);
    }

    /// <summary>
    /// Plants a crop on the tile under the drone and pays its seed items. A wild crop on the tile is replaced.
    /// The checks run in order and the first failure is returned without changing the field or the items.
    /// </summary>
    /// <param name="crop">The crop to plant.</param>
    /// <returns>The result of the planting, see <see cref="PlantResult"/>.</returns>
    public PlantResult Plant(Crop crop)
    {
        if (crop.Seed is null)
        {
            return PlantResult.NotPlantable;
        }

        Tile tile = DroneTile;
        if (tile.Crop?.Seed is not null)
        {
            return PlantResult.TileOccupied;
        }

        if (tile.Ground != crop.Ground)
        {
            return PlantResult.WrongGround;
        }

        if (_items.GetValueOrDefault(crop.Seed.Item) < crop.Seed.Count)
        {
            return PlantResult.NotEnoughItems;
        }

        int itemsLeft = _items[crop.Seed.Item] - crop.Seed.Count;
        if (itemsLeft == 0)
        {
            _items.Remove(crop.Seed.Item);
        }
        else
        {
            _items[crop.Seed.Item] = itemsLeft;
        }

        DroneTile = new Tile(tile.Ground, crop, 0);
        return PlantResult.Planted;
    }

    /// <summary>
    /// Returns a larger field with this field's tiles in its top-left corner and ripe wild crop on every other tile.
    /// The drone keeps its position, and the new field shares this field's item store.
    /// </summary>
    /// <param name="width">The new number of columns. Must not be smaller than the current width.</param>
    /// <param name="height">The new number of rows. Must not be smaller than the current height.</param>
    /// <param name="wildCrop">The crop that grows wild, ripe, on every new tile.</param>
    /// <returns>The new, larger field.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The new width or height is smaller than the current one.</exception>
    public Field Grown(int width, int height, Crop wildCrop)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, Width);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, Height);
        Tile[,] tiles = CreateWildTiles(width, height, wildCrop);
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                tiles[x, y] = _tiles[x, y];
            }
        }

        return new Field(tiles, DronePosition, _items);
    }

    private static Tile[,] CreateWildTiles(int width, int height, Crop wildCrop)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        var tiles = new Tile[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tiles[x, y] = new Tile(Ground.Grass, wildCrop, wildCrop.GrowTimeTicks);
            }
        }

        return tiles;
    }

    private static int WrapIndex(int index, int size)
    {
        return ((index % size) + size) % size;
    }
}
