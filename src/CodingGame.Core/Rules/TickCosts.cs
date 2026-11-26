namespace CodingGame.Core.Rules;

/// <summary>
/// How many simulation ticks each drone action and script step takes.
/// </summary>
/// <param name="MoveTicks">Ticks for one move of the drone.</param>
/// <param name="HarvestTicks">Ticks for one harvest.</param>
/// <param name="PlantTicks">Ticks for one planting.</param>
/// <param name="RipeTicks">Ticks for checking whether a crop is ripe.</param>
/// <param name="TillTicks">Ticks for tilling one tile of ground.</param>
/// <param name="GroundTicks">Ticks for checking what a tile is made of.</param>
/// <param name="LoopIterationTicks">Ticks for each iteration of a loop.</param>
/// <param name="FunctionCallTicks">Ticks for each function call.</param>
/// <param name="SpinTicks">Ticks for one spin of the drone.</param>
/// <param name="AssignTicks">Ticks for one assignment to a variable.</param>
public sealed record TickCosts(
    int MoveTicks,
    int HarvestTicks,
    int PlantTicks,
    int RipeTicks,
    int TillTicks,
    int GroundTicks,
    int LoopIterationTicks,
    int FunctionCallTicks,
    int SpinTicks,
    int AssignTicks);
