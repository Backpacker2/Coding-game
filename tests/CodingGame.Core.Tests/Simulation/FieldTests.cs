using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;
using Xunit;

namespace CodingGame.Core.Tests.Simulation;

public class FieldTests
{
    private static readonly Crop _clover = new("CLOVER", Ground.Grass, 200, 1, null, true);
    private static readonly Crop _radish = new("RADISH", Ground.Grass, 300, 4, new SeedCost("CLOVER", 2), false);
    private static readonly Crop _potato = new("POTATO", Ground.Soil, 500, 6, new SeedCost("RADISH", 2), false);

    [Fact]
    public void NewFieldHasGivenSizeAndDroneAtOrigin()
    {
        var field = new Field(3, 1, _clover);

        Assert.Equal(3, field.Width);
        Assert.Equal(1, field.Height);
        Assert.Equal(new TilePosition(0, 0), field.DronePosition);
    }

    [Fact]
    public void NewFieldFillsEveryTileWithRipeWildCrop()
    {
        var field = new Field(3, 1, _clover);

        Assert.Equal(new Tile(Ground.Grass, _clover, 200), field.GetTile(2, 0));
    }

    [Fact]
    public void GetTileOutsideFieldThrows()
    {
        var field = new Field(3, 1, _clover);

        Assert.Throws<ArgumentOutOfRangeException>(() => field.GetTile(3, 0));
    }

    [Fact]
    public void FieldWithZeroWidthThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Field(0, 1, _clover));
    }

    [Fact]
    public void MoveEastMovesDroneOneTile()
    {
        var field = new Field(3, 1, _clover);

        field.Move(Direction.East);

        Assert.Equal(new TilePosition(1, 0), field.DronePosition);
    }

    [Fact]
    public void MoveEastWrapsToFirstColumn()
    {
        var field = new Field(3, 1, _clover);

        field.Move(Direction.East);
        field.Move(Direction.East);
        field.Move(Direction.East);

        Assert.Equal(new TilePosition(0, 0), field.DronePosition);
    }

    [Fact]
    public void MoveWestFromStartWrapsToLastColumn()
    {
        var field = new Field(3, 1, _clover);

        field.Move(Direction.West);

        Assert.Equal(new TilePosition(2, 0), field.DronePosition);
    }

    [Fact]
    public void MoveNorthFromStartWrapsToLastRowAndSouthReturns()
    {
        var field = new Field(3, 3, _clover);

        field.Move(Direction.North);
        Assert.Equal(new TilePosition(0, 2), field.DronePosition);

        field.Move(Direction.South);
        Assert.Equal(new TilePosition(0, 0), field.DronePosition);
    }

    [Fact]
    public void MoveNorthOnSingleRowStaysInPlace()
    {
        var field = new Field(3, 1, _clover);

        field.Move(Direction.North);

        Assert.Equal(new TilePosition(0, 0), field.DronePosition);
    }

    [Fact]
    public void NewFieldDroneStandsOnRipeGrassWithNoItems()
    {
        var field = new Field(3, 1, _clover);

        Assert.True(field.IsRipe());
        Assert.Equal(Ground.Grass, field.GetGround());
        Assert.Empty(field.Items);
    }

    [Fact]
    public void AdvanceWithNegativeTicksThrows()
    {
        var field = new Field(3, 1, _clover);

        Assert.Throws<ArgumentOutOfRangeException>(() => field.Advance(-1));
    }

    [Fact]
    public void HarvestRipeCloverGivesItemAndCropStaysAtAgeZero()
    {
        var field = new Field(3, 1, _clover);

        field.Harvest();

        Assert.Equal(1, field.Items["CLOVER"]);
        Assert.Equal(new Tile(Ground.Grass, _clover, 0), field.GetTile(0, 0));
        Assert.False(field.IsRipe());
    }

    [Fact]
    public void HarvestingUnripeCloverAgainGivesNothingExtra()
    {
        var field = new Field(3, 1, _clover);
        field.Harvest();

        field.Harvest();

        Assert.Equal(1, field.Items["CLOVER"]);
        Assert.Equal(0, field.GetTile(0, 0).CropAgeTicks);
    }

    [Fact]
    public void CloverRipensAfterGrowTimeTicks()
    {
        var field = new Field(3, 1, _clover);
        field.Harvest();

        field.Advance(199);
        Assert.False(field.IsRipe());

        field.Advance(1);
        Assert.True(field.IsRipe());

        field.Advance(1000);
        Assert.Equal(200, field.GetTile(0, 0).CropAgeTicks);
    }

    [Fact]
    public void HarvestRipeRadishGivesYieldAndRemovesCrop()
    {
        var field = new Field(3, 1, _radish);

        field.Harvest();

        Assert.Equal(4, field.Items["RADISH"]);
        Assert.Null(field.GetTile(0, 0).Crop);
    }

    [Fact]
    public void HarvestEmptyTileChangesNothing()
    {
        var field = new Field(3, 1, _clover);
        field.Till();
        field.Till();

        field.Harvest();

        Assert.Empty(field.Items);
        Assert.Equal(new Tile(Ground.Grass, null, 0), field.GetTile(0, 0));
    }

    [Fact]
    public void TillSwitchesGroundAndRemovesCrop()
    {
        var field = new Field(3, 1, _clover);

        field.Till();
        Assert.Equal(new Tile(Ground.Soil, null, 0), field.GetTile(0, 0));
        Assert.Equal(Ground.Soil, field.GetGround());

        field.Till();
        Assert.Equal(new Tile(Ground.Grass, null, 0), field.GetTile(0, 0));
    }

    [Fact]
    public void PlantingWildCropIsNotPlantable()
    {
        var field = new Field(3, 1, _clover);

        Assert.Equal(PlantResult.NotPlantable, field.Plant(_clover));
    }

    [Fact]
    public void PlantOnTileWithCropIsTileOccupied()
    {
        var field = new Field(3, 1, _clover);

        Assert.Equal(PlantResult.TileOccupied, field.Plant(_radish));
    }

    [Fact]
    public void PlantOnEmptyGrassWithSeedItemsIsPlanted()
    {
        var field = FieldWithThreeClover();
        field.Till();
        field.Till();

        Assert.Equal(PlantResult.Planted, field.Plant(_radish));
        Assert.Equal(new Tile(Ground.Grass, _radish, 0), field.GetTile(0, 0));
        Assert.Equal(1, field.Items["CLOVER"]);
    }

    [Fact]
    public void PlantWithTooFewSeedItemsIsNotEnoughItems()
    {
        var field = new Field(3, 1, _clover);
        field.Harvest();
        field.Move(Direction.East);
        field.Till();
        field.Till();

        Assert.Equal(PlantResult.NotEnoughItems, field.Plant(_radish));
        Assert.Equal(1, field.Items["CLOVER"]);
        Assert.Null(field.GetTile(1, 0).Crop);
    }

    [Fact]
    public void PotatoOnGrassIsWrongGroundAndOnSoilNeedsRadishSeeds()
    {
        var field = FieldWithThreeClover();
        field.Till();
        field.Till();

        Assert.Equal(PlantResult.WrongGround, field.Plant(_potato));

        field.Till();
        Assert.Equal(PlantResult.NotEnoughItems, field.Plant(_potato));
    }

    [Fact]
    public void UnripeRadishHarvestGivesNothingAndRemovesCrop()
    {
        var field = FieldWithThreeClover();
        field.Till();
        field.Till();
        field.Plant(_radish);

        field.Advance(299);
        field.Harvest();

        Assert.Null(field.GetTile(0, 0).Crop);
        Assert.Equal(0, field.Items.GetValueOrDefault("RADISH"));
    }

    [Fact]
    public void RipeRadishHarvestGivesYieldAndEmptiesTile()
    {
        var field = FieldWithThreeClover();
        field.Till();
        field.Till();
        field.Plant(_radish);

        field.Advance(300);
        field.Harvest();

        Assert.Equal(4, field.Items["RADISH"]);
        Assert.Null(field.GetTile(0, 0).Crop);
    }

    [Fact]
    public void SameCallSequenceGivesSameTiles()
    {
        var first = new Field(3, 1, _clover);
        var second = new Field(3, 1, _clover);

        ApplyScript(first);
        ApplyScript(second);

        for (int x = 0; x < 3; x++)
        {
            Assert.Equal(first.GetTile(x, 0), second.GetTile(x, 0));
        }
    }

    private static Field FieldWithThreeClover()
    {
        var field = new Field(3, 1, _clover);
        for (int i = 0; i < 3; i++)
        {
            field.Harvest();
            field.Move(Direction.East);
        }

        return field;
    }

    private static void ApplyScript(Field field)
    {
        field.Harvest();
        field.Move(Direction.East);
        field.Till();
        field.Till();
        field.Plant(_radish);
        field.Advance(150);
        field.Move(Direction.West);
        field.Harvest();
    }

    [Fact]
    public void AdvanceByIntMaxValueRipensWithoutOverflow()
    {
        var field = new Field(3, 1, _clover);
        field.Harvest();
        field.Advance(150);

        field.Advance(int.MaxValue);

        Assert.Equal(200, field.GetTile(0, 0).CropAgeTicks);
        Assert.True(field.IsRipe());
    }

    [Fact]
    public void PlantingWithExactlyTheSeedCountLeavesNoItemEntry()
    {
        var field = new Field(3, 1, _clover);
        field.Harvest();
        field.Move(Direction.East);
        field.Harvest();
        field.Till();
        field.Till();

        PlantResult result = field.Plant(_radish);

        Assert.Equal(PlantResult.Planted, result);
        Assert.Empty(field.Items);
    }

    [Fact]
    public void HarvestWritesYieldToSharedItemDictionary()
    {
        var items = new Dictionary<string, int>();
        var field = new Field(3, 1, _clover, items);

        field.Harvest();

        Assert.Equal(1, items["CLOVER"]);
    }

    [Fact]
    public void PlantTakesSeedItemsFromSharedItemDictionary()
    {
        var items = new Dictionary<string, int> { ["CLOVER"] = 2 };
        var field = new Field(3, 1, _clover, items);
        field.Till();
        field.Till();

        Assert.Equal(PlantResult.Planted, field.Plant(_radish));
        Assert.False(items.ContainsKey("CLOVER"));
    }

    [Fact]
    public void FieldsSharingItemDictionarySeeEachOthersItems()
    {
        var items = new Dictionary<string, int>();
        var first = new Field(3, 1, _clover, items);
        var second = new Field(3, 1, _clover, items);

        first.Harvest();
        Assert.Equal(1, second.Items["CLOVER"]);

        second.Harvest();
        Assert.Equal(2, first.Items["CLOVER"]);
    }
}
