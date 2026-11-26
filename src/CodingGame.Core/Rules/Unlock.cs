namespace CodingGame.Core.Rules;

/// <summary>
/// The items an unlock costs.
/// </summary>
/// <param name="Item">The name of the item that is paid. It is the name of a crop.</param>
/// <param name="Count">How many of the item are paid.</param>
public sealed record UnlockPrice(string Item, int Count);

/// <summary>
/// Something the player buys with items.
/// </summary>
/// <param name="Id">The unique name of the unlock, such as FOR_LOOP.</param>
/// <param name="Title">The name the player sees.</param>
/// <param name="Price">What the unlock costs.</param>
/// <param name="Features">The names a script may use once this is bought: keywords (for, while, if, def), the operator ==, drone commands and constants. May be empty.</param>
/// <param name="FieldSize">The size the field grows to when this is bought, or null when it does not change the field.</param>
public sealed record Unlock(string Id, string Title, UnlockPrice Price, IReadOnlyList<string> Features, FieldSize? FieldSize);
