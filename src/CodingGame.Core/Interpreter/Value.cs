using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;

namespace CodingGame.Core.Interpreter;

/// <summary>
/// The kinds of value a script can hold.
/// </summary>
public enum ValueKind
{
    /// <summary>No value. Commands that only act return this.</summary>
    None,

    /// <summary>A whole number.</summary>
    Number,

    /// <summary>True or false.</summary>
    Boolean,

    /// <summary>A compass direction such as NORTH.</summary>
    Direction,

    /// <summary>What a tile is made of, such as SOIL.</summary>
    Ground,

    /// <summary>A kind of crop, such as RADISH.</summary>
    Crop,
}

/// <summary>
/// A value a script can hold. Create one with a <c>From</c> method and read it with the matching <c>As</c> method.
/// </summary>
public readonly record struct Value
{
    /// <summary>
    /// Gets the value that holds nothing. The default value of <see cref="Value"/> is the same.
    /// </summary>
    public static Value None => default;

    /// <summary>
    /// Gets the kind of value this is.
    /// </summary>
    public ValueKind Kind { get; private init; }

    private int Payload { get; init; }

    private Crop? CropPayload { get; init; }

    /// <summary>
    /// Creates a number value.
    /// </summary>
    /// <param name="number">The number to hold.</param>
    /// <returns>A value of kind <see cref="ValueKind.Number"/>.</returns>
    public static Value FromNumber(int number)
    {
        return new Value { Kind = ValueKind.Number, Payload = number };
    }

    /// <summary>
    /// Creates a boolean value.
    /// </summary>
    /// <param name="value">The boolean to hold.</param>
    /// <returns>A value of kind <see cref="ValueKind.Boolean"/>.</returns>
    public static Value FromBoolean(bool value)
    {
        return new Value { Kind = ValueKind.Boolean, Payload = value ? 1 : 0 };
    }

    /// <summary>
    /// Creates a direction value.
    /// </summary>
    /// <param name="direction">The direction to hold.</param>
    /// <returns>A value of kind <see cref="ValueKind.Direction"/>.</returns>
    public static Value FromDirection(Direction direction)
    {
        return new Value { Kind = ValueKind.Direction, Payload = (int)direction };
    }

    /// <summary>
    /// Creates a ground value.
    /// </summary>
    /// <param name="ground">The ground to hold.</param>
    /// <returns>A value of kind <see cref="ValueKind.Ground"/>.</returns>
    public static Value FromGround(Ground ground)
    {
        return new Value { Kind = ValueKind.Ground, Payload = (int)ground };
    }

    /// <summary>
    /// Creates a crop value.
    /// </summary>
    /// <param name="crop">The crop to hold.</param>
    /// <returns>A value of kind <see cref="ValueKind.Crop"/>.</returns>
    public static Value FromCrop(Crop crop)
    {
        return new Value { Kind = ValueKind.Crop, CropPayload = crop };
    }

    /// <summary>
    /// Reads the value as a number.
    /// </summary>
    /// <returns>The number held by this value.</returns>
    /// <exception cref="InvalidOperationException">The value is not a number. This is a bug in the game, not in a script.</exception>
    public int AsNumber()
    {
        RequireKind(ValueKind.Number);
        return Payload;
    }

    /// <summary>
    /// Reads the value as a boolean.
    /// </summary>
    /// <returns>The boolean held by this value.</returns>
    /// <exception cref="InvalidOperationException">The value is not a boolean. This is a bug in the game, not in a script.</exception>
    public bool AsBoolean()
    {
        RequireKind(ValueKind.Boolean);
        return Payload != 0;
    }

    /// <summary>
    /// Reads the value as a direction.
    /// </summary>
    /// <returns>The direction held by this value.</returns>
    /// <exception cref="InvalidOperationException">The value is not a direction. This is a bug in the game, not in a script.</exception>
    public Direction AsDirection()
    {
        RequireKind(ValueKind.Direction);
        return (Direction)Payload;
    }

    /// <summary>
    /// Reads the value as a ground.
    /// </summary>
    /// <returns>The ground held by this value.</returns>
    /// <exception cref="InvalidOperationException">The value is not a ground. This is a bug in the game, not in a script.</exception>
    public Ground AsGround()
    {
        RequireKind(ValueKind.Ground);
        return (Ground)Payload;
    }

    /// <summary>
    /// Reads the value as a crop.
    /// </summary>
    /// <returns>The crop held by this value.</returns>
    /// <exception cref="InvalidOperationException">The value is not a crop. This is a bug in the game, not in a script.</exception>
    public Crop AsCrop()
    {
        RequireKind(ValueKind.Crop);
        return CropPayload!;
    }

    private void RequireKind(ValueKind expected)
    {
        if (Kind != expected)
        {
            throw new InvalidOperationException($"Value is {Kind}, not {expected}");
        }
    }
}
