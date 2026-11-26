using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;

namespace CodingGame.Core.Session;

/// <summary>
/// What the player owns: the items from harvests and the unlocks the player has bought.
/// It is kept in memory only, so closing the game resets it.
/// </summary>
public sealed class GameProgress
{
    private readonly Dictionary<string, int> _items = [];
    private readonly HashSet<string> _boughtIds = [];

    /// <summary>
    /// Creates the progress of a new player: no items and nothing bought.
    /// </summary>
    /// <param name="rules">The game rules, with the unlocks, the crops and the starting field.</param>
    public GameProgress(GameRules rules)
    {
        Rules = rules;
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
    /// Gets the ids of the unlocks the player has bought.
    /// </summary>
    public IReadOnlySet<string> BoughtIds => _boughtIds;

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
    /// Pays the price of an unlock from the items and marks the unlock as bought.
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
    /// Creates a field for a new run: the field size of this progress, with the wild crop ripe on every
    /// tile and the drone at (0, 0). The field keeps its items in this progress, so harvests and seed
    /// payments change the bank.
    /// </summary>
    /// <returns>The new field.</returns>
    public Field NewField()
    {
        Crop wildCrop = Rules.Crops.First(crop => crop.Seed is null);
        FieldSize size = FieldSize;
        return new Field(size.Width, size.Height, wildCrop, _items);
    }
}
