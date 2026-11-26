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
    public void BuyingBiggerFieldGrowsNewFields()
    {
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 30);

        progress.Buy(UnlockNamed("BIGGER_FIELD"));
        Field field = progress.NewField();

        Assert.Equal(new FieldSize(5, 5), progress.FieldSize);
        Assert.Equal(5, field.Width);
        Assert.Equal(5, field.Height);
        Assert.Equal("CLOVER", field.GetTile(4, 4).Crop?.Name);
    }

    [Fact]
    public void ItemsFromAFieldStayInProgressAcrossFields()
    {
        var progress = new GameProgress(_rules);
        Field first = progress.NewField();

        first.Harvest();
        Field second = progress.NewField();

        Assert.Equal(1, progress.Items["CLOVER"]);
        Assert.Equal("CLOVER", second.GetTile(0, 0).Crop?.Name);
    }

    [Fact]
    public void AddItemsRejectsZero()
    {
        var progress = new GameProgress(_rules);

        Assert.Throws<ArgumentOutOfRangeException>(() => progress.AddItems("CLOVER", 0));
    }

    private static Unlock UnlockNamed(string id)
    {
        return _rules.Unlocks.Single(unlock => unlock.Id == id);
    }
}
