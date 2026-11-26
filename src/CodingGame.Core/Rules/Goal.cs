namespace CodingGame.Core.Rules;

/// <summary>
/// What a goal asks the player to reach.
/// </summary>
public enum GoalKind
{
    /// <summary>A run ran the script to its end.</summary>
    FinishRun,

    /// <summary>The player holds at least a number of one item.</summary>
    HaveItems,

    /// <summary>The drone stood on every tile during one run.</summary>
    VisitAllTiles,

    /// <summary>The player bought at least one unlock.</summary>
    BuyUnlock,
}

/// <summary>
/// A first-steps goal the player is shown and that is checked from the game state.
/// </summary>
/// <param name="Id">The unique name of the goal, such as FINISH_RUN.</param>
/// <param name="Text">The goal the player reads, such as "Have 3 clover.".</param>
/// <param name="Hint">A short tip on how to reach the goal.</param>
/// <param name="Kind">What the goal checks.</param>
/// <param name="Item">The name of the item the goal counts, a crop name, for <see cref="GoalKind.HaveItems"/>; otherwise null.</param>
/// <param name="Count">How many of <paramref name="Item"/> are needed, for <see cref="GoalKind.HaveItems"/>; otherwise 0.</param>
public sealed record Goal(string Id, string Text, string Hint, GoalKind Kind, string? Item, int Count);
