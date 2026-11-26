using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using CodingGame.Core.Simulation;
using CodingGame.Core.Tests.Interpreter;
using Xunit;

namespace CodingGame.Core.Tests.Session;

public class GameProgressTests
{
    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    [Fact]
    public void NewProgressHasNoItemsNothingBoughtAndStartSize()
    {
        var progress = new GameProgress(_rules);

        Assert.Empty(progress.Items);
        Assert.All(_rules.Unlocks, unlock => Assert.False(progress.IsBought(unlock)));
        Assert.Equal(new FieldSize(3, 3), progress.FieldSize);
    }

    [Theory]
    [InlineData("for")]
    [InlineData("while")]
    [InlineData("if")]
    [InlineData("def")]
    [InlineData("==")]
    [InlineData("plant")]
    [InlineData("RADISH")]
    [InlineData("ripe")]
    [InlineData("till")]
    [InlineData("ground")]
    [InlineData("POTATO")]
    [InlineData("SOIL")]
    [InlineData("GRASS")]
    public void NewProgressLocksFeaturesOfEveryUnlock(string feature)
    {
        var progress = new GameProgress(_rules);

        Assert.Contains(feature, progress.LockedFeatures);
    }

    [Fact]
    public void NewProgressDoesNotLockFreeFeatures()
    {
        var progress = new GameProgress(_rules);

        Assert.DoesNotContain("move", progress.LockedFeatures);
    }

    [Fact]
    public void CanBuyNeedsEnoughItems()
    {
        var progress = new GameProgress(_rules);
        Unlock forLoop = UnlockNamed("FOR_LOOP");

        progress.AddItems("CLOVER", 9);

        Assert.False(progress.CanBuy(forLoop));

        progress.AddItems("CLOVER", 1);

        Assert.True(progress.CanBuy(forLoop));
    }

    [Fact]
    public void BuyPaysAndUnlocksFeatures()
    {
        var progress = new GameProgress(_rules);
        Unlock forLoop = UnlockNamed("FOR_LOOP");
        progress.AddItems("CLOVER", 12);

        progress.Buy(forLoop);

        Assert.Equal(2, progress.Items["CLOVER"]);
        Assert.True(progress.IsBought(forLoop));
        Assert.DoesNotContain("for", progress.LockedFeatures);
    }

    [Fact]
    public void BuyWithExactPriceRemovesItemKey()
    {
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 10);

        progress.Buy(UnlockNamed("FOR_LOOP"));

        Assert.False(progress.Items.ContainsKey("CLOVER"));
    }

    [Fact]
    public void BuyTwiceThrows()
    {
        var progress = new GameProgress(_rules);
        Unlock forLoop = UnlockNamed("FOR_LOOP");
        progress.AddItems("CLOVER", 20);
        progress.Buy(forLoop);

        Assert.Throws<InvalidOperationException>(() => progress.Buy(forLoop));
    }

    [Fact]
    public void BuyWithoutItemsThrows()
    {
        var progress = new GameProgress(_rules);

        Assert.Throws<InvalidOperationException>(() => progress.Buy(UnlockNamed("FOR_LOOP")));
    }

    [Fact]
    public void BuyingBiggerFieldGrowsTheField()
    {
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 30);

        progress.Buy(UnlockNamed("BIGGER_FIELD"));

        Assert.Equal(new FieldSize(5, 5), progress.FieldSize);
        Assert.Equal(5, progress.Field.Width);
        Assert.Equal(5, progress.Field.Height);
        Assert.Equal("CLOVER", progress.Field.GetTile(4, 4).Crop?.Name);
    }

    [Fact]
    public void BuyingBiggerFieldKeepsPlantedTilesWithTheirAge()
    {
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 32);
        progress.Field.Till();
        progress.Field.Till();
        progress.Field.Plant(RadishCrop());
        progress.Field.Advance(50);
        progress.Field.Move(Direction.East);

        progress.Buy(UnlockNamed("BIGGER_FIELD"));

        Assert.Equal(new Tile(Ground.Grass, RadishCrop(), 50), progress.Field.GetTile(0, 0));
        Assert.Equal(new Tile(Ground.Grass, CloverCrop(), 200), progress.Field.GetTile(4, 4));
        Assert.Equal(new TilePosition(1, 0), progress.Field.DronePosition);
    }

    [Fact]
    public void ItemsFromAFieldStayInProgress()
    {
        var progress = new GameProgress(_rules);

        progress.Field.Harvest();

        Assert.Equal(1, progress.Items["CLOVER"]);
        Assert.Equal("CLOVER", progress.Field.GetTile(0, 0).Crop?.Name);
    }

    [Fact]
    public void AddItemsRejectsZero()
    {
        var progress = new GameProgress(_rules);

        Assert.Throws<ArgumentOutOfRangeException>(() => progress.AddItems("CLOVER", 0));
    }

    [Fact]
    public void ResetEmptiesItemsAndBoughtUnlocks()
    {
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 10);
        progress.Buy(UnlockNamed("FOR_LOOP"));

        progress.Reset();

        Assert.Empty(progress.Items);
        Assert.False(progress.IsBought(UnlockNamed("FOR_LOOP")));
        Assert.Contains("for", progress.LockedFeatures);
    }

    [Fact]
    public void ResetShrinksTheFieldBackToTheStartSize()
    {
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 30);
        progress.Buy(UnlockNamed("BIGGER_FIELD"));
        Assert.Equal(new FieldSize(5, 5), progress.FieldSize);

        progress.Reset();

        Assert.Equal(_rules.StartField, progress.FieldSize);
    }

    [Fact]
    public void ResetGivesAFreshStartField()
    {
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 32);
        progress.Field.Till();
        progress.Field.Till();
        progress.Field.Plant(RadishCrop());
        progress.Buy(UnlockNamed("BIGGER_FIELD"));
        progress.Field.Move(Direction.East);

        progress.Reset();

        Assert.Equal(3, progress.Field.Width);
        Assert.Equal(new TilePosition(0, 0), progress.Field.DronePosition);
        Assert.Equal(new Tile(Ground.Grass, CloverCrop(), 200), progress.Field.GetTile(0, 0));
    }

    [Fact]
    public void ResetEmptiesTheItemsOfAFieldMadeEarlier()
    {
        var progress = new GameProgress(_rules);
        Field field = progress.Field;
        progress.AddItems("CLOVER", 3);
        Assert.NotEmpty(field.Items);

        progress.Reset();

        Assert.Empty(field.Items);
    }

    [Fact]
    public void ResetOnANewProgressChangesNothing()
    {
        var progress = new GameProgress(_rules);

        progress.Reset();

        Assert.Empty(progress.Items);
        Assert.All(_rules.Unlocks, unlock => Assert.False(progress.IsBought(unlock)));
        Assert.Equal(new FieldSize(3, 3), progress.FieldSize);
    }

    [Fact]
    public void RestoredProgressHasSavedItemsAndUnlocks()
    {
        var items = new Dictionary<string, int> { ["CLOVER"] = 5 };
        var bought = new HashSet<string> { "FOR_LOOP" };
        var progress = new GameProgress(_rules, items, bought, null, default);

        Assert.Equal(5, progress.Items["CLOVER"]);
        Assert.True(progress.IsBought(UnlockNamed("FOR_LOOP")));
        Assert.False(progress.IsBought(UnlockNamed("WHILE_LOOP")));
        Assert.DoesNotContain("for", progress.LockedFeatures);
    }

    [Fact]
    public void RestoredProgressWithoutTilesHasAFreshFieldOfSavedSize()
    {
        var progress = new GameProgress(_rules, new Dictionary<string, int>(), new HashSet<string> { "BIGGER_FIELD" }, null, default);

        Assert.Equal(new FieldSize(5, 5), progress.FieldSize);
        Assert.Equal(5, progress.Field.Width);
        Assert.Equal(new Tile(Ground.Grass, CloverCrop(), 200), progress.Field.GetTile(4, 4));
    }

    [Fact]
    public void RestoredProgressKeepsSavedTilesAndDrone()
    {
        var tiles = new Tile[3, 3];
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                tiles[x, y] = new Tile(Ground.Grass, null, 0);
            }
        }

        tiles[2, 1] = new Tile(Ground.Grass, CloverCrop(), 77);
        var progress = new GameProgress(_rules, new Dictionary<string, int>(), new HashSet<string>(), tiles, new TilePosition(2, 1));

        Assert.Equal(new Tile(Ground.Grass, CloverCrop(), 77), progress.Field.GetTile(2, 1));
        Assert.Equal(new TilePosition(2, 1), progress.Field.DronePosition);
    }

    [Fact]
    public void RestoredProgressRejectsTilesOfAnotherSize()
    {
        var tiles = new Tile[2, 2];

        Assert.Throws<ArgumentException>(() => new GameProgress(_rules, new Dictionary<string, int>(), new HashSet<string>(), tiles, default));
    }

    [Fact]
    public void RestoredProgressDoesNotShareTheSourceDictionary()
    {
        var items = new Dictionary<string, int> { ["CLOVER"] = 5 };
        var progress = new GameProgress(_rules, items, new HashSet<string>(), null, default);

        items["CLOVER"] = 99;

        Assert.Equal(5, progress.Items["CLOVER"]);
    }

    private static Unlock UnlockNamed(string id)
    {
        return _rules.Unlocks.Single(unlock => unlock.Id == id);
    }

    private static Crop CloverCrop()
    {
        return _rules.Crops.Single(crop => crop.Name == "CLOVER");
    }

    private static Crop RadishCrop()
    {
        return _rules.Crops.Single(crop => crop.Name == "RADISH");
    }
}
