namespace CodingGame.Core.Session;

/// <summary>
/// The stats of one item for one ended run: how many more of the item the player holds than at the start
/// of the run, and the rate of that gain per 1,000 ticks.
/// </summary>
/// <param name="Item">The name of the item, which is the name of a crop.</param>
/// <param name="Gained">The number of items gained during the run. At least 1.</param>
/// <param name="PerThousandTicks">The gain per 1,000 ticks the run used, rounded down. It can be 0 for a small gain over a long run.</param>
/// <param name="IsNewBest">Whether the rate is higher than the best rate kept for the item before this run, or there was none.</param>
public sealed record ItemRate(string Item, int Gained, int PerThousandTicks, bool IsNewBest);

/// <summary>
/// The stats of one ended run: the ticks it used and the rate of every item it gained.
/// </summary>
/// <param name="TicksUsed">The number of ticks the run used. 0 when the run did not use a tick.</param>
/// <param name="Rates">The items the run gained, sorted by item name in ordinal order. Empty when the run used no ticks or gained nothing.</param>
public sealed record RunStats(int TicksUsed, IReadOnlyList<ItemRate> Rates);
