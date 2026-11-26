using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;

namespace CodingGame.Core.Session;

/// <summary>
/// What the player owns: the items from harvests, the unlocks the player has bought and the field with its crops.
/// It is saved to the save file with <see cref="SaveFile"/> and loaded again when the game starts.
/// </summary>
public sealed class GameProgress
{
    private readonly Dictionary<string, int> _items = [];
    private readonly HashSet<string> _boughtIds = [];

    /// <summary>
    /// Creates the progress of a new player: no items, nothing bought and the start field.
    /// </summary>
    /// <param name="rules">The game rules, with the unlocks, the crops and the starting field.</param>
    public GameProgress(GameRules rules)
        : this(rules, new Dictionary<string, int>(), new HashSet<string>(), null, default)
    {
    }

    /// <summary>
    /// Creates progress with items, bought unlocks and a field that were checked by the caller (the save loader).
    /// The field keeps its items in this progress.
    /// </summary>
    /// <param name="rules">The game rules, with the unlocks, the crops and the starting field.</param>
    /// <param name="items">Item name to count. The caller guarantees every name is a crop and every count is at least 1.</param>
    /// <param name="boughtUnlockIds">Ids of bought unlocks. The caller guarantees each id is in <paramref name="rules"/>.Unlocks.</param>
    /// <param name="tiles">The tiles of the field, in the size that the bought unlocks give. Null gives a fresh field of that size.</param>
    /// <param name="dronePosition">The tile the drone stands on. Not used when <paramref name="tiles"/> is null.</param>
    /// <exception cref="ArgumentException">The size of <paramref name="tiles"/> is not the size of the field that the bought unlocks give.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The drone is outside the field.</exception>
    public GameProgress(
        GameRules rules,
        IReadOnlyDictionary<string, int> items,
        IReadOnlySet<string> boughtUnlockIds,
        Tile[,]? tiles,
        TilePosition dronePosition)
    {
        Rules = rules;
        foreach (KeyValuePair<string, int> item in items)
        {
            _items[item.Key] = item.Value;
        }

        _boughtIds.UnionWith(boughtUnlockIds);
        if (tiles is null)
        {
            Field = CreateFreshField();
        }
        else
        {
            FieldSize size = FieldSize;
            if (tiles.GetLength(0) != size.Width || tiles.GetLength(1) != size.Height)
            {
                throw new ArgumentException($"the tiles are {tiles.GetLength(0)}x{tiles.GetLength(1)}, the field is {size.Width}x{size.Height}", nameof(tiles));
            }

            Field = new Field(tiles, dronePosition, _items);
        }
    }

    /// <summary>
    /// Gets the game rules, with the unlocks, the crops and the starting field.
    /// </summary>
    public GameRules Rules { get; }

    /// <summary>
    /// Gets the items the player owns, keyed by item name. Every count is at least 1: an item the player
    /// has none of is not listed.
    /// </summary>
    public IReadOnlyDictionary<string, int> Items => _items;

    /// <summary>
    /// Gets the field the player works on. It lasts from run to run and is changed by the runs and by buying a bigger field.
    /// </summary>
    public Field Field { get; private set; }

    /// <summary>
    /// Gets the features that are still locked: the features of every unlock that is not bought yet.
    /// Each call returns a new set.
    /// </summary>
    public IReadOnlySet<string> LockedFeatures
    {
        get
        {
            var locked = new HashSet<string>();
            foreach (Unlock unlock in Rules.Unlocks.Where(unlock => !IsBought(unlock)))
            {
                locked.UnionWith(unlock.Features);
            }

            return locked;
        }
    }

    /// <summary>
    /// Gets the size of the field the player gets: the field size of the last bought unlock that has one,
    /// or the starting field size when none has.
    /// </summary>
    public FieldSize FieldSize
    {
        get
        {
            FieldSize size = Rules.StartField;
            foreach (Unlock unlock in Rules.Unlocks)
            {
                if (IsBought(unlock) && unlock.FieldSize is { } grownSize)
                {
                    size = grownSize;
                }
            }

            return size;
        }
    }

    private Crop WildCrop => Rules.Crops.First(crop => crop.Seed is null);

    /// <summary>
    /// Checks whether the player has bought an unlock.
    /// </summary>
    /// <param name="unlock">The unlock to check.</param>
    /// <returns>True when the unlock is bought.</returns>
    public bool IsBought(Unlock unlock)
    {
        return _boughtIds.Contains(unlock.Id);
    }

    /// <summary>
    /// Checks whether the player can buy an unlock now: it is not bought yet and the player has
    /// enough of the price item.
    /// </summary>
    /// <param name="unlock">The unlock to check.</param>
    /// <returns>True when <see cref="Buy"/> would succeed.</returns>
    public bool CanBuy(Unlock unlock)
    {
        return !IsBought(unlock) && _items.GetValueOrDefault(unlock.Price.Item) >= unlock.Price.Count;
    }

    /// <summary>
    /// Pays the price of an unlock from the items and marks the unlock as bought. A bought field size grows
    /// the field in place: its tiles and the drone stay as they are.
    /// </summary>
    /// <param name="unlock">The unlock to buy.</param>
    /// <exception cref="InvalidOperationException">The unlock is already bought, or the player has too few of the price item.</exception>
    public void Buy(Unlock unlock)
    {
        if (IsBought(unlock))
        {
            throw new InvalidOperationException($"unlock {unlock.Id} is already bought");
        }

        if (!CanBuy(unlock))
        {
            throw new InvalidOperationException($"not enough {unlock.Price.Item} to buy unlock {unlock.Id}");
        }

        FieldSize sizeBefore = FieldSize;
        int itemsLeft = _items[unlock.Price.Item] - unlock.Price.Count;
        if (itemsLeft == 0)
        {
            _items.Remove(unlock.Price.Item);
        }
        else
        {
            _items[unlock.Price.Item] = itemsLeft;
        }

        _boughtIds.Add(unlock.Id);
        FieldSize sizeAfter = FieldSize;
        if (sizeAfter != sizeBefore)
        {
            Field = Field.Grown(sizeAfter.Width, sizeAfter.Height, WildCrop);
        }
    }

    /// <summary>
    /// Adds items to the player's bank, for example as a reward.
    /// </summary>
    /// <param name="item">The name of the item, which is the name of a crop.</param>
    /// <param name="count">How many items to add. Must be at least 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">The count is less than 1.</exception>
    public void AddItems(string item, int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        _items[item] = _items.GetValueOrDefault(item) + count;
    }

    /// <summary>
    /// Puts the progress back to the state of a new player: no items, nothing bought and a fresh start field.
    /// The item store is emptied in place.
    /// </summary>
    public void Reset()
    {
        _items.Clear();
        _boughtIds.Clear();
        Field = CreateFreshField();
    }

    private Field CreateFreshField()
    {
        FieldSize size = FieldSize;
        return new Field(size.Width, size.Height, WildCrop, _items);
    }
}
