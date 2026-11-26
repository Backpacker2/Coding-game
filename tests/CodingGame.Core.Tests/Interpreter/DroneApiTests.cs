using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter;

public class DroneApiTests
{
    private static readonly GameRules _rules = GameRules.Load(Path.Combine(AppContext.BaseDirectory, "data"));
    private static readonly string[] _commandNames = ["ground", "harvest", "move", "plant", "ripe", "till"];

    [Fact]
    public void ConstantsHaveNineEntries()
    {
        var api = new DroneApi(_rules, NewField());

        Assert.Equal(9, api.Constants.Count);
    }

    [Fact]
    public void ConstantsDoNotIncludeCommands()
    {
        var api = new DroneApi(_rules, NewField());

        Assert.False(api.Constants.ContainsKey("move"));
    }

    [Fact]
    public void NorthConstantIsNorthDirection()
    {
        var api = new DroneApi(_rules, NewField());

        Assert.Equal(Value.FromDirection(Direction.North), api.Constants["NORTH"]);
    }

    [Fact]
    public void SoilConstantIsSoilGround()
    {
        var api = new DroneApi(_rules, NewField());

        Assert.Equal(Value.FromGround(Ground.Soil), api.Constants["SOIL"]);
    }

    [Fact]
    public void RadishConstantIsRadishCrop()
    {
        var api = new DroneApi(_rules, NewField());

        Assert.Equal(Value.FromCrop(CropNamed("RADISH")), api.Constants["RADISH"]);
    }

    [Fact]
    public void CommandNamesAreTheSixDroneCommands()
    {
        var api = new DroneApi(_rules, NewField());

        Assert.Equal(
            _commandNames,
            api.Commands.Keys.Order());
    }

    [Fact]
    public void CommandCostsComeFromTickCosts()
    {
        var api = new DroneApi(_rules, NewField());
        var ticks = _rules.TickCosts;

        Assert.Equal(ticks.MoveTicks, api.Commands["move"].CostTicks);
        Assert.Equal(ticks.HarvestTicks, api.Commands["harvest"].CostTicks);
        Assert.Equal(ticks.PlantTicks, api.Commands["plant"].CostTicks);
        Assert.Equal(ticks.RipeTicks, api.Commands["ripe"].CostTicks);
        Assert.Equal(ticks.TillTicks, api.Commands["till"].CostTicks);
        Assert.Equal(ticks.GroundTicks, api.Commands["ground"].CostTicks);
    }

    [Fact]
    public void LoopAndFunctionCallTicksComeFromTickCosts()
    {
        var api = new DroneApi(_rules, NewField());

        Assert.Equal(_rules.TickCosts.LoopIterationTicks, api.LoopIterationTicks);
        Assert.Equal(_rules.TickCosts.FunctionCallTicks, api.FunctionCallTicks);
    }

    [Fact]
    public void MoveTakesOneDirection()
    {
        var api = new DroneApi(_rules, NewField());

        Assert.Equal(new[] { ValueKind.Direction }, api.Commands["move"].ArgumentKinds);
    }

    [Fact]
    public void PlantTakesOneCrop()
    {
        var api = new DroneApi(_rules, NewField());

        Assert.Equal(new[] { ValueKind.Crop }, api.Commands["plant"].ArgumentKinds);
    }

    [Fact]
    public void HarvestTakesNoArguments()
    {
        var api = new DroneApi(_rules, NewField());

        Assert.Empty(api.Commands["harvest"].ArgumentKinds);
    }

    [Fact]
    public void MoveEastMovesTheDroneOnTheField()
    {
        Field field = NewField();
        var api = new DroneApi(_rules, field);

        CommandOutcome outcome = api.Commands["move"].Run([Value.FromDirection(Direction.East)]);

        Assert.Equal(new TilePosition(1, 0), field.DronePosition);
        Assert.Null(outcome.FailureDetail);
    }

    [Fact]
    public void HarvestAddsCloverToItems()
    {
        Field field = NewField();
        var api = new DroneApi(_rules, field);

        api.Commands["harvest"].Run([]);

        Assert.Equal(1, field.Items["CLOVER"]);
    }

    [Fact]
    public void RipeIsTrueOnRipeWildCrop()
    {
        var api = new DroneApi(_rules, NewField());

        CommandOutcome outcome = api.Commands["ripe"].Run([]);

        Assert.Equal(Value.FromBoolean(true), outcome.Result);
    }

    [Fact]
    public void RipeIsFalseAfterHarvest()
    {
        var api = new DroneApi(_rules, NewField());
        api.Commands["harvest"].Run([]);

        CommandOutcome outcome = api.Commands["ripe"].Run([]);

        Assert.Equal(Value.FromBoolean(false), outcome.Result);
    }

    [Fact]
    public void GroundIsGrassThenSoilAfterTill()
    {
        var api = new DroneApi(_rules, NewField());

        CommandOutcome before = api.Commands["ground"].Run([]);
        api.Commands["till"].Run([]);
        CommandOutcome after = api.Commands["ground"].Run([]);

        Assert.Equal(Value.FromGround(Ground.Grass), before.Result);
        Assert.Equal(Value.FromGround(Ground.Soil), after.Result);
    }

    [Fact]
    public void PlantOnTileWithPlantedCropFails()
    {
        Field field = NewField();
        field.Harvest();
        field.Move(Direction.East);
        field.Harvest();
        field.Till();
        field.Till();
        field.Plant(CropNamed("RADISH"));
        var api = new DroneApi(_rules, field);

        CommandOutcome outcome = api.Commands["plant"].Run([Value.FromCrop(CropNamed("RADISH"))]);

        Assert.Equal("cannot plant RADISH: the tile already has a crop", outcome.FailureDetail);
    }

    [Fact]
    public void PlantOnWrongGroundFails()
    {
        Field field = NewField();
        field.Till();
        var api = new DroneApi(_rules, field);

        CommandOutcome outcome = api.Commands["plant"].Run([Value.FromCrop(CropNamed("RADISH"))]);

        Assert.Equal("cannot plant RADISH: it does not grow on this ground", outcome.FailureDetail);
    }

    [Fact]
    public void PlantWithoutSeedItemsFails()
    {
        Field field = NewField();
        field.Till();
        field.Till();
        var api = new DroneApi(_rules, field);

        CommandOutcome outcome = api.Commands["plant"].Run([Value.FromCrop(CropNamed("RADISH"))]);

        Assert.Equal("cannot plant RADISH: not enough seed items", outcome.FailureDetail);
    }

    [Fact]
    public void PlantWildCropFails()
    {
        Field field = NewField();
        field.Till();
        field.Till();
        var api = new DroneApi(_rules, field);

        CommandOutcome outcome = api.Commands["plant"].Run([Value.FromCrop(CropNamed("CLOVER"))]);

        Assert.Equal("cannot plant CLOVER: it grows wild", outcome.FailureDetail);
    }

    [Fact]
    public void PlantRadishOnGrassWithSeedItemsSucceeds()
    {
        Field field = NewField();
        field.Harvest();
        field.Move(Direction.East);
        field.Harvest();
        field.Till();
        field.Till();
        var api = new DroneApi(_rules, field);

        CommandOutcome outcome = api.Commands["plant"].Run([Value.FromCrop(CropNamed("RADISH"))]);

        Assert.Null(outcome.FailureDetail);
        Assert.Equal(new Tile(Ground.Grass, CropNamed("RADISH"), 0), field.GetTile(1, 0));
    }

    private static Crop CropNamed(string name)
    {
        return _rules.Crops.Single(crop => crop.Name == name);
    }

    private static Field NewField()
    {
        return new Field(3, 1, CropNamed("CLOVER"));
    }
}
