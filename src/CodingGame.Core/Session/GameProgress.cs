using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;

namespace CodingGame.Core.Session;

/// <summary>
/// What the player owns: the items from harvests, the unlocks the player has bought, the field with its crops,
/// the first-steps goals that are done and the best rate per 1,000 ticks for each item. It is saved to the save file with <see cref="SaveFile"/> and loaded again when the game starts.
/// </summary>
public sealed class GameProgress
{
    private readonly Dictionary<string, int> _items = [];
    private readonly Dictionary<string, int> _bestRates = [];
    private readonly HashSet<string> _boughtIds = [];
    private readonly HashSet<string> _doneGoalIds = [];

    /// <summary>
    /// Creates the progress of a new player: no items, nothing bought and the start field.
    /// </summary>
    /// <param name="rules">The game rules, with the unlocks, the crops and the starting field.</param>
    public GameProgress(GameRules rules)
        : this(rules, new Dictionary<string, int>(), new HashSet<string>(), new HashSet<string>(), new Dictionary<string, int>(), null, default)
    {
    }

    /// <summary>
    /// Creates progress with items, bought unlocks and a field that were checked by the caller (the save loader).
    /// The field keeps its items in this progress.
    /// </summary>
    /// <param name="rules">The game rules, with the unlocks, the crops and the starting field.</param>
    /// <param name="items">Item name to count. The caller guarantees every name is a crop and every count is at least 1.</param>
    /// <param name="boughtUnlockIds">Ids of bought unlocks. The caller guarantees each id is in <paramref name="rules"/>.Unlocks.</param>
    /// <param name="doneGoalIds">Ids of the goals that are done.</param>
    /// <param name="bestRates">Item name to the best rate per 1,000 ticks. Each name must be a crop and each rate must be at least 1.</param>
    /// <param name="tiles">The tiles of the field, in the size that the bought unlocks give. Null gives a fresh field of that size.</param>
    /// <param name="dronePosition">The tile the drone stands on. Not used when <paramref name="tiles"/> is null.</param>
    /// <exception cref="ArgumentException">A goal id in <paramref name="doneGoalIds"/> is not a goal in the rules, a best rate names an item that is not a crop or is below 1, or the size of <paramref name="tiles"/> is not the size of the field that the bought unlocks give.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The drone is outside the field.</exception>
    public GameProgress(
        GameRules rules,
        IReadOnlyDictionary<string, int> items,
        IReadOnlySet<string> boughtUnlockIds,
        IReadOnlySet<string> doneGoalIds,
        IReadOnlyDictionary<string, int> bestRates,
        Tile[,]? tiles,
        TilePosition dronePosition)
    {
        Rules = rules;
        foreach (KeyValuePair<string, int> item in items)
        {
            _items[item.Key] = item.Value;
        }

        foreach (KeyValuePair<string, int> best in bestRates)
        {
            if (!rules.Crops.Any(crop => crop.Name == best.Key))
            {
                throw new ArgumentException($"unknown item {best.Key}", nameof(bestRates));
            }

            if (best.Value < 1)
            {
                throw new ArgumentException($"{best.Key}: best rate must be at least 1, was {best.Value}", nameof(bestRates));
            }

            _bestRates[best.Key] = best.Value;
        }

        _boughtIds.UnionWith(boughtUnlockIds);
        foreach (string goalId in doneGoalIds)
        {
            if (!rules.Goals.Any(goal => goal.Id == goalId))
            {
                throw new ArgumentException($"unknown goal id {goalId}", nameof(doneGoalIds));
            }
        }

        _doneGoalIds.UnionWith(doneGoalIds);
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
    /// Gets the best rate per 1,000 ticks that the player has had for each item, keyed by item name. Every rate is at least 1.
    /// An item the player has no best for is not listed.
    /// </summary>
    public IReadOnlyDictionary<string, int> BestRates => _bestRates;

    /// <summary>
    /// Gets the field the player works on. It lasts from run to run and is changed by the runs and by buying a bigger field.
    /// </summary>
    public Field Field { get; private set; }

    /// <summary>
    /// Gets the features that are still locked, each with the unlock that gives it: the features of every unlock that is not bought yet.
    /// Each call returns a new dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, Unlock> LockedFeatures
    {
        get
        {
            var locked = new Dictionary<string, Unlock>();
            foreach (Unlock unlock in Rules.Unlocks.Where(unlock => !IsBought(unlock)))
            {
                foreach (string feature in unlock.Features)
                {
                    locked.TryAdd(feature, unlock);
                }
            }

            return locked;
        }
    }

    /// <summary>
    /// Gets the first goal, in goals.json order, that is not done yet. Null when every goal is done.
    /// </summary>
    public Goal? CurrentGoal => Rules.Goals.FirstOrDefault(goal => !IsDone(goal));

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
    /// Checks whether the player has reached a goal. A goal that is done stays done.
    /// </summary>
    /// <param name="goal">The goal to check.</param>
    /// <returns>True when the goal is done.</returns>
    public bool IsDone(Goal goal)
    {
        return _doneGoalIds.Contains(goal.Id);
    }

    /// <summary>
    /// Checks the goals against the run that has just ended or was stopped, and against the current items and unlocks.
    /// Call it once when a run ends or is stopped, before the progress is saved.
    /// </summary>
    /// <param name="endedRun">The run that ended or was stopped. A stopped run counts for the goals about tiles and items, not for finishing.</param>
    /// <returns>The goals that became done now, in goals.json order. Empty when none did.</returns>
    public IReadOnlyList<Goal> CheckGoals(RunSession endedRun)
    {
        return MarkMetGoals(endedRun);
    }

    /// <summary>
    /// Works out the rate per 1,000 ticks of every item an ended run gained, and keeps a rate as the best for its item
    /// when it is higher than the best kept so far. The rate is the gain times 1,000 divided by the ticks the run used,
    /// rounded down. A rate of 0 is reported but is never kept as a best. Call it once when a run ends or is stopped,
    /// before the progress is saved.
    /// </summary>
    /// <param name="endedRun">The run that ended or was stopped. Every ended run counts, whatever the reason it stopped.</param>
    /// <returns>The ticks the run used and the rate of each item it gained, sorted by item name. Empty when the run used no ticks.</returns>
    public RunStats RecordRunStats(RunSession endedRun)
    {
        int ticksUsed = endedRun.TicksUsed;
        if (ticksUsed == 0)
        {
            return new RunStats(0, []);
        }

        var rates = new List<ItemRate>();
        foreach (KeyValuePair<string, int> entry in endedRun.Field.Items.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            int gained = entry.Value - endedRun.ItemsAtStart.GetValueOrDefault(entry.Key);
            if (gained <= 0)
            {
                continue;
            }

            int perThousandTicks = gained * 1000 / ticksUsed;
            bool isNewBest = perThousandTicks >= 1 && (!_bestRates.TryGetValue(entry.Key, out int best) || perThousandTicks > best);
            if (isNewBest)
            {
                _bestRates[entry.Key] = perThousandTicks;
            }

            rates.Add(new ItemRate(entry.Key, gained, perThousandTicks, isNewBest));
        }

        return new RunStats(ticksUsed, rates);
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

        MarkMetGoals(null);
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
    /// Puts the progress back to the state of a new player: no items, no best rate, nothing bought, no goal done and a fresh start field.
    /// The item store is emptied in place.
    /// </summary>
    public void Reset()
    {
        _items.Clear();
        _bestRates.Clear();
        _boughtIds.Clear();
        _doneGoalIds.Clear();
        Field = CreateFreshField();
    }

    private Field CreateFreshField()
    {
        FieldSize size = FieldSize;
        return new Field(size.Width, size.Height, WildCrop, _items);
    }

    private List<Goal> MarkMetGoals(RunSession? endedRun)
    {
        var becameDone = new List<Goal>();
        foreach (Goal goal in Rules.Goals)
        {
            if (!IsDone(goal) && IsMet(goal, endedRun))
            {
                _doneGoalIds.Add(goal.Id);
                becameDone.Add(goal);
            }
        }

        return becameDone;
    }

    private bool IsMet(Goal goal, RunSession? endedRun)
    {
        return goal.Kind switch
        {
            GoalKind.FinishRun => endedRun?.Status == StepStatus.Finished,
            GoalKind.HaveItems => goal.Item is { } item && _items.GetValueOrDefault(item) >= goal.Count,
            GoalKind.VisitAllTiles => endedRun?.VisitedAllTiles == true,
            GoalKind.BuyUnlock => Rules.Unlocks.Any(IsBought),
            _ => throw new ArgumentOutOfRangeException(nameof(goal), goal.Kind, "unknown goal kind"),
        };
    }
}
