using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;

namespace CodingGame.Core.Interpreter;

/// <summary>
/// The result of running a drone command: the value it gives back and, when it broke a game rule, why.
/// </summary>
/// <param name="Result">The value the command gives back. <see cref="Value.None"/> for a command that only acts.</param>
/// <param name="FailureDetail">The reason the command failed, or null when it succeeded.</param>
public readonly record struct CommandOutcome(Value Result, string? FailureDetail)
{
    /// <summary>
    /// Creates the outcome of a command that succeeded and gives back a value.
    /// </summary>
    /// <param name="result">The value the command gives back.</param>
    /// <returns>A successful outcome holding the value.</returns>
    public static CommandOutcome Done(Value result)
    {
        return new CommandOutcome(result, null);
    }

    /// <summary>
    /// Creates the outcome of a command that succeeded and gives back nothing.
    /// </summary>
    /// <returns>A successful outcome holding <see cref="Value.None"/>.</returns>
    public static CommandOutcome Done()
    {
        return new CommandOutcome(Value.None, null);
    }

    /// <summary>
    /// Creates the outcome of a command that broke a game rule.
    /// </summary>
    /// <param name="detail">The reason the command failed, in words the player can read.</param>
    /// <returns>A failed outcome holding <see cref="Value.None"/>.</returns>
    public static CommandOutcome Failed(string detail)
    {
        return new CommandOutcome(Value.None, detail);
    }
}

/// <summary>
/// A drone command as a script can call it.
/// </summary>
/// <param name="ArgumentKinds">The kind of each argument, in order. A call with another number or kind of arguments is an error.</param>
/// <param name="CostTicks">How many ticks the call costs, from the tick costs in the data folder.</param>
/// <param name="Run">Runs the command on the field with the checked arguments. It does not advance the field's time.</param>
public sealed record DroneCommand(
    IReadOnlyList<ValueKind> ArgumentKinds,
    int CostTicks,
    Func<IReadOnlyList<Value>, CommandOutcome> Run);

/// <summary>
/// The one table of drone commands and constants that every script language uses.
/// </summary>
public sealed class DroneApi
{
    /// <summary>
    /// Creates the drone API for a field, with the costs from the game rules.
    /// </summary>
    /// <param name="rules">The game rules that give the tick cost of each command and the crops that are constants.</param>
    /// <param name="field">The field the commands act on.</param>
    public DroneApi(GameRules rules, Field field)
    {
        Field = field;
        TickCosts ticks = rules.TickCosts;
        LoopIterationTicks = ticks.LoopIterationTicks;
        FunctionCallTicks = ticks.FunctionCallTicks;

        Commands = new Dictionary<string, DroneCommand>
        {
            ["move"] = new([ValueKind.Direction], ticks.MoveTicks, args =>
            {
                field.Move(args[0].AsDirection());
                return CommandOutcome.Done();
            }),
            ["harvest"] = new([], ticks.HarvestTicks, _ =>
            {
                field.Harvest();
                return CommandOutcome.Done();
            }),
            ["plant"] = new([ValueKind.Crop], ticks.PlantTicks, args => Plant(field, args[0].AsCrop())),
            ["ripe"] = new([], ticks.RipeTicks, _ => CommandOutcome.Done(Value.FromBoolean(field.IsRipe()))),
            ["till"] = new([], ticks.TillTicks, _ =>
            {
                field.Till();
                return CommandOutcome.Done();
            }),
            ["ground"] = new([], ticks.GroundTicks, _ => CommandOutcome.Done(Value.FromGround(field.GetGround()))),
            ["spin"] = new([], ticks.SpinTicks, _ =>
            {
                field.Spin();
                return CommandOutcome.Done();
            }),
        };

        var constants = new Dictionary<string, Value>
        {
            ["NORTH"] = Value.FromDirection(Direction.North),
            ["EAST"] = Value.FromDirection(Direction.East),
            ["SOUTH"] = Value.FromDirection(Direction.South),
            ["WEST"] = Value.FromDirection(Direction.West),
            ["GRASS"] = Value.FromGround(Ground.Grass),
            ["SOIL"] = Value.FromGround(Ground.Soil),
        };
        foreach (Crop crop in rules.Crops)
        {
            constants[crop.Name] = Value.FromCrop(crop);
        }

        Constants = constants;
    }

    /// <summary>
    /// Gets the field the commands act on. The evaluator advances its time, one tick per running step.
    /// </summary>
    public Field Field { get; }

    /// <summary>
    /// Gets the ticks that each iteration of a loop costs, from the tick costs in the data folder.
    /// </summary>
    public int LoopIterationTicks { get; }

    /// <summary>
    /// Gets the ticks that each call of a player function costs, from the tick costs in the data folder.
    /// </summary>
    public int FunctionCallTicks { get; }

    /// <summary>
    /// Gets the drone commands by the name a script calls them by.
    /// </summary>
    public IReadOnlyDictionary<string, DroneCommand> Commands { get; }

    /// <summary>
    /// Gets the constants by the name a script writes them with, such as NORTH or RADISH.
    /// </summary>
    public IReadOnlyDictionary<string, Value> Constants { get; }

    private static CommandOutcome Plant(Field field, Crop crop)
    {
        return field.Plant(crop) switch
        {
            PlantResult.Planted => CommandOutcome.Done(),
            PlantResult.NotPlantable => CommandOutcome.Failed($"cannot plant {crop.Name}: it grows wild"),
            PlantResult.TileOccupied => CommandOutcome.Failed($"cannot plant {crop.Name}: the tile already has a crop"),
            PlantResult.WrongGround => CommandOutcome.Failed($"cannot plant {crop.Name}: it does not grow on this ground"),
            PlantResult.NotEnoughItems => CommandOutcome.Failed($"cannot plant {crop.Name}: not enough seed items"),
            _ => throw new ArgumentOutOfRangeException(nameof(crop), crop, "unknown planting result"),
        };
    }
}
