using CodingGame.Core.Rules;
using Xunit;

namespace CodingGame.Core.Tests.Rules;

public class GameRulesUnlockTests
{
    private const string ValidCropsJson = """[{"name":"CLOVER","ground":"GRASS","growTimeTicks":200,"yieldItems":1,"seed":null,"regrows":true},{"name":"RADISH","ground":"GRASS","growTimeTicks":300,"yieldItems":4,"seed":{"item":"CLOVER","count":2},"regrows":false}]""";

    private const string ValidTicksJson = """{"moveTicks":20,"harvestTicks":20,"plantTicks":20,"ripeTicks":1,"tillTicks":20,"groundTicks":1,"loopIterationTicks":1,"functionCallTicks":1,"spinTicks":10}""";

    private const string ValidFieldJson = """{"width":3,"height":3}""";

    private const string ForLoopJson = """{"id":"FOR_LOOP","title":"for loop","price":{"item":"CLOVER","count":10},"features":["for"],"fieldSize":null}""";

    [Fact]
    public void EmptyUnlockListIsAllowed()
    {
        GameRules rules = LoadWith("[]");

        Assert.Empty(rules.Unlocks);
    }

    [Fact]
    public void DuplicateUnlockIdIsReported()
    {
        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith($"[{ForLoopJson},{ForLoopJson}]"));

        Assert.Equal("unlocks.json: duplicate unlock id FOR_LOOP", exception.Message);
    }

    [Fact]
    public void PriceItemThatIsNotACropIsReported()
    {
        const string json = """[{"id":"FOR_LOOP","title":"for loop","price":{"item":"CARROT","count":10},"features":["for"],"fieldSize":null}]""";

        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(json));

        Assert.Equal("unlocks.json: FOR_LOOP: price item CARROT is not a crop", exception.Message);
    }

    [Fact]
    public void NonPositivePriceCountIsReported()
    {
        const string json = """[{"id":"FOR_LOOP","title":"for loop","price":{"item":"CLOVER","count":0},"features":["for"],"fieldSize":null}]""";

        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(json));

        Assert.Equal("unlocks.json: FOR_LOOP: price.count must be greater than 0, was 0", exception.Message);
    }

    [Fact]
    public void FeatureInTwoUnlocksIsReported()
    {
        const string secondForUnlock = """{"id":"OTHER","title":"other","price":{"item":"CLOVER","count":10},"features":["for"],"fieldSize":null}""";

        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith($"[{ForLoopJson},{secondForUnlock}]"));

        Assert.Equal("unlocks.json: feature for is in more than one unlock", exception.Message);
    }

    [Fact]
    public void UnlockWithoutFeaturesAndFieldSizeIsReported()
    {
        const string json = """[{"id":"NOTHING","title":"nothing","price":{"item":"CLOVER","count":10},"features":[],"fieldSize":null}]""";

        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(json));

        Assert.Equal("unlocks.json: NOTHING: unlock does nothing", exception.Message);
    }

    [Theory]
    [InlineData("""{"width":3,"height":5}""")]
    [InlineData("""{"width":2,"height":5}""")]
    [InlineData("""{"width":3,"height":3}""")]
    public void FieldSizeNotLargerThanStartFieldIsReported(string fieldSizeJson)
    {
        string json = $$"""[{"id":"BIGGER","title":"bigger","price":{"item":"CLOVER","count":10},"features":[],"fieldSize":{{fieldSizeJson}}}]""";

        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(json));

        Assert.Equal("unlocks.json: BIGGER: fieldSize must be larger than the start field", exception.Message);
    }

    [Fact]
    public void FieldSizeLargerThanStartFieldIsAccepted()
    {
        const string json = """[{"id":"BIGGER","title":"bigger","price":{"item":"CLOVER","count":10},"features":[],"fieldSize":{"width":4,"height":4}}]""";

        GameRules rules = LoadWith(json);

        Assert.Equal(new FieldSize(4, 4), rules.Unlocks[0].FieldSize);
    }

    private static GameRules LoadWith(string unlocksJson)
    {
        string folder = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(folder, "crops.json"), ValidCropsJson);
            File.WriteAllText(Path.Combine(folder, "ticks.json"), ValidTicksJson);
            File.WriteAllText(Path.Combine(folder, "field.json"), ValidFieldJson);
            File.WriteAllText(Path.Combine(folder, "unlocks.json"), unlocksJson);
            File.WriteAllText(Path.Combine(folder, "goals.json"), "[]");
            File.WriteAllText(Path.Combine(folder, "hints.json"), "[]");
            File.WriteAllText(Path.Combine(folder, "starter.py"), "harvest()\n");
            File.WriteAllText(Path.Combine(folder, "starter.js"), "harvest();\n");
            return GameRules.Load(folder);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
