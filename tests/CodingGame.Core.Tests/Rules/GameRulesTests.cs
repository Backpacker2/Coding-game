using CodingGame.Core.Rules;
using Xunit;

namespace CodingGame.Core.Tests.Rules;

public class GameRulesTests
{
    private const string ValidCropsJson = """[{"name":"CLOVER","ground":"GRASS","growTimeTicks":200,"yieldItems":1,"seed":null,"regrows":true},{"name":"RADISH","ground":"GRASS","growTimeTicks":300,"yieldItems":4,"seed":{"item":"CLOVER","count":2},"regrows":false}]""";

    private const string ValidTicksJson = """{"moveTicks":20,"harvestTicks":20,"plantTicks":20,"ripeTicks":1,"tillTicks":20,"groundTicks":1,"loopIterationTicks":1,"functionCallTicks":1}""";

    [Fact]
    public void LoadsThreeCropsFromDataFolder()
    {
        GameRules rules = GameRules.Load(Path.Combine(AppContext.BaseDirectory, "data"));

        Assert.Equal(3, rules.Crops.Count);
    }

    [Fact]
    public void RadishGrowsOnGrassWithCloverSeedAndDoesNotRegrow()
    {
        GameRules rules = GameRules.Load(Path.Combine(AppContext.BaseDirectory, "data"));

        Crop radish = rules.Crops.Single(crop => crop.Name == "RADISH");

        Assert.Equal(new Crop("RADISH", Ground.Grass, 300, 4, new SeedCost("CLOVER", 2), false), radish);
    }

    [Fact]
    public void CloverHasNoSeedCost()
    {
        GameRules rules = GameRules.Load(Path.Combine(AppContext.BaseDirectory, "data"));

        Crop clover = rules.Crops.Single(crop => crop.Name == "CLOVER");

        Assert.Null(clover.Seed);
    }

    [Fact]
    public void PotatoGrowsOnSoil()
    {
        GameRules rules = GameRules.Load(Path.Combine(AppContext.BaseDirectory, "data"));

        Crop potato = rules.Crops.Single(crop => crop.Name == "POTATO");

        Assert.Equal(Ground.Soil, potato.Ground);
    }

    [Fact]
    public void TickCostsComeFromDataFolder()
    {
        GameRules rules = GameRules.Load(Path.Combine(AppContext.BaseDirectory, "data"));

        Assert.Equal(20, rules.TickCosts.MoveTicks);
        Assert.Equal(1, rules.TickCosts.RipeTicks);
    }

    [Fact]
    public void MissingCropsFileIsReported()
    {
        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(null, ValidTicksJson));

        Assert.Contains("crops.json", exception.Message);
    }

    [Fact]
    public void MissingTicksFileIsReported()
    {
        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(ValidCropsJson, null));

        Assert.Contains("ticks.json", exception.Message);
    }

    [Fact]
    public void InvalidCropsJsonIsReported()
    {
        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith("{", ValidTicksJson));

        Assert.Contains("crops.json", exception.Message);
    }

    [Theory]
    [InlineData("""[{"name":"CLOVER","ground":"GRASS","yieldItems":1,"seed":null,"regrows":true}]""", "growTimeTicks")]
    [InlineData("""[{"name":"CLOVER","ground":"WATER","growTimeTicks":200,"yieldItems":1,"seed":null,"regrows":true}]""", "crops.json")]
    [InlineData("""[{"name":"CLOVER","ground":"GRASS","growTimeTicks":200,"yieldItems":1,"seed":null,"regrows":true,"color":"green"}]""", "crops.json")]
    [InlineData("""[{"name":"CLOVER","ground":"GRASS","growTimeTicks":0,"yieldItems":1,"seed":null,"regrows":true}]""", "growTimeTicks must be greater than 0")]
    [InlineData("""[{"name":"CLOVER","ground":"GRASS","growTimeTicks":-5,"yieldItems":1,"seed":null,"regrows":true}]""", "growTimeTicks must be greater than 0")]
    [InlineData("""[{"name":"CLOVER","ground":"GRASS","growTimeTicks":200,"yieldItems":1,"seed":null,"regrows":true},{"name":"RADISH","ground":"GRASS","growTimeTicks":300,"yieldItems":4,"seed":{"item":"CLOVER","count":0},"regrows":false}]""", "must be greater than 0")]
    [InlineData("""[{"name":"CLOVER","ground":"GRASS","growTimeTicks":200,"yieldItems":1,"seed":null,"regrows":true},{"name":"CLOVER","ground":"GRASS","growTimeTicks":200,"yieldItems":1,"seed":null,"regrows":true}]""", "duplicate crop name")]
    [InlineData("""[{"name":"RADISH","ground":"GRASS","growTimeTicks":300,"yieldItems":4,"seed":{"item":"CARROT","count":2},"regrows":false}]""", "is not a crop")]
    [InlineData("[]", "no crops")]
    public void InvalidCropsAreReported(string cropsJson, string expectedText)
    {
        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(cropsJson, ValidTicksJson));

        Assert.Contains(expectedText, exception.Message);
    }

    [Theory]
    [InlineData("""{"moveTicks":0,"harvestTicks":20,"plantTicks":20,"ripeTicks":1,"tillTicks":20,"groundTicks":1,"loopIterationTicks":1,"functionCallTicks":1}""", "moveTicks must be greater than 0")]
    [InlineData("""{"moveTicks":20,"harvestTicks":20,"plantTicks":20,"ripeTicks":1,"tillTicks":20,"groundTicks":1,"functionCallTicks":1}""", "loopIterationTicks")]
    public void InvalidTickCostsAreReported(string ticksJson, string expectedText)
    {
        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(ValidCropsJson, ticksJson));

        Assert.Contains(expectedText, exception.Message);
    }

    private static GameRules LoadWith(string? cropsJson, string? ticksJson)
    {
        string folder = Directory.CreateTempSubdirectory().FullName;
        try
        {
            if (cropsJson is not null)
            {
                File.WriteAllText(Path.Combine(folder, "crops.json"), cropsJson);
            }

            if (ticksJson is not null)
            {
                File.WriteAllText(Path.Combine(folder, "ticks.json"), ticksJson);
            }

            return GameRules.Load(folder);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
