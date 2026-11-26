using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using Xunit;

namespace CodingGame.Core.Tests.Rules;

public class GameRulesTests
{
    private const string ValidCropsJson = """[{"name":"CLOVER","ground":"GRASS","growTimeTicks":200,"yieldItems":1,"seed":null,"regrows":true},{"name":"RADISH","ground":"GRASS","growTimeTicks":300,"yieldItems":4,"seed":{"item":"CLOVER","count":2},"regrows":false}]""";

    private const string ValidTicksJson = """{"moveTicks":20,"harvestTicks":20,"plantTicks":20,"ripeTicks":1,"tillTicks":20,"groundTicks":1,"loopIterationTicks":1,"functionCallTicks":1}""";

    private const string ValidFieldJson = """{"width":3,"height":3}""";

    private const string ValidUnlocksJson = "[]";

    private const string ValidGoalsJson = "[]";

    private const string ValidHintsJson = "[]";

    private const string ValidPythonStarter = "harvest()\n";

    private const string ValidJavaScriptStarter = "harvest();\n";

    [Fact]
    public void LoadsThreeCropsFromDataFolder()
    {
        GameRules rules = GameRules.Load(Path.Combine(AppContext.BaseDirectory, "data"));

        Assert.Equal(3, rules.Crops.Count);
    }

    [Fact]
    public void StartFieldComesFromDataFolder()
    {
        GameRules rules = GameRules.Load(Path.Combine(AppContext.BaseDirectory, "data"));

        Assert.Equal(3, rules.StartField.Width);
        Assert.Equal(3, rules.StartField.Height);
    }

    [Fact]
    public void MissingFieldFileIsReported()
    {
        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(ValidCropsJson, ValidTicksJson, null));

        Assert.Contains("field.json", exception.Message);
    }

    [Fact]
    public void NonPositiveFieldSizeIsReported()
    {
        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(ValidCropsJson, ValidTicksJson, """{"width":0,"height":3}"""));

        Assert.Contains("width", exception.Message);
    }

    [Fact]
    public void NoWildCropIsReported()
    {
        const string everyCropIsSeeded = """[{"name":"CLOVER","ground":"GRASS","growTimeTicks":200,"yieldItems":1,"seed":{"item":"RADISH","count":2},"regrows":true},{"name":"RADISH","ground":"GRASS","growTimeTicks":300,"yieldItems":4,"seed":{"item":"CLOVER","count":2},"regrows":false}]""";

        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(everyCropIsSeeded, ValidTicksJson));

        Assert.Contains("no wild crop", exception.Message);
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
    public void LoadsSevenUnlocksFromDataFolder()
    {
        GameRules rules = GameRules.Load(Path.Combine(AppContext.BaseDirectory, "data"));

        Assert.Equal(7, rules.Unlocks.Count);
    }

    [Fact]
    public void ForLoopUnlockCostsCloverAndOpensFor()
    {
        GameRules rules = GameRules.Load(Path.Combine(AppContext.BaseDirectory, "data"));

        Unlock unlock = rules.Unlocks[0];

        Assert.Equal("FOR_LOOP", unlock.Id);
        Assert.Equal(new UnlockPrice("CLOVER", 10), unlock.Price);
        Assert.Equal("for", Assert.Single(unlock.Features));
        Assert.Null(unlock.FieldSize);
    }

    [Fact]
    public void BiggerFieldUnlockGrowsFieldToFiveByFive()
    {
        GameRules rules = GameRules.Load(Path.Combine(AppContext.BaseDirectory, "data"));

        Unlock unlock = rules.Unlocks[1];

        Assert.Equal(new FieldSize(5, 5), unlock.FieldSize);
        Assert.Empty(unlock.Features);
    }

    [Fact]
    public void MissingUnlocksFileIsReported()
    {
        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(ValidCropsJson, ValidTicksJson, ValidFieldJson, null));

        Assert.Contains("unlocks.json", exception.Message);
        Assert.Contains("file not found", exception.Message);
    }

    [Fact]
    public void InvalidUnlocksJsonIsReported()
    {
        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(ValidCropsJson, ValidTicksJson, ValidFieldJson, "{"));

        Assert.Contains("unlocks.json", exception.Message);
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

    [Fact]
    public void StarterTextsComeFromDataFolder()
    {
        GameRules rules = GameRules.Load(Path.Combine(AppContext.BaseDirectory, "data"));

        Assert.Equal(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "starter.py")), rules.StarterText(ScriptLanguage.Python));
        Assert.Equal(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "starter.js")), rules.StarterText(ScriptLanguage.JavaScript));
    }

    [Fact]
    public void StarterTextIsTheTextOfTheStarterFile()
    {
        GameRules rules = LoadWith(ValidCropsJson, ValidTicksJson, ValidFieldJson, ValidUnlocksJson, "say(1)\n", "say(2);\n");

        Assert.Equal("say(1)\n", rules.StarterText(ScriptLanguage.Python));
        Assert.Equal("say(2);\n", rules.StarterText(ScriptLanguage.JavaScript));
    }

    [Fact]
    public void UnknownLanguageHasNoStarterText()
    {
        GameRules rules = GameRules.Load(Path.Combine(AppContext.BaseDirectory, "data"));

        Assert.Throws<ArgumentOutOfRangeException>(() => rules.StarterText((ScriptLanguage)99));
    }

    [Fact]
    public void MissingPythonStarterFileIsReported()
    {
        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(ValidCropsJson, ValidTicksJson, pythonStarter: null));

        Assert.Equal("starter.py: file not found", exception.Message);
    }

    [Fact]
    public void MissingJavaScriptStarterFileIsReported()
    {
        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(ValidCropsJson, ValidTicksJson, javaScriptStarter: null));

        Assert.Equal("starter.js: file not found", exception.Message);
    }

    private static GameRules LoadWith(
        string? cropsJson,
        string? ticksJson,
        string? fieldJson = ValidFieldJson,
        string? unlocksJson = ValidUnlocksJson,
        string? pythonStarter = ValidPythonStarter,
        string? javaScriptStarter = ValidJavaScriptStarter,
        string? goalsJson = ValidGoalsJson,
        string? hintsJson = ValidHintsJson)
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

            if (fieldJson is not null)
            {
                File.WriteAllText(Path.Combine(folder, "field.json"), fieldJson);
            }

            if (unlocksJson is not null)
            {
                File.WriteAllText(Path.Combine(folder, "unlocks.json"), unlocksJson);
            }

            if (pythonStarter is not null)
            {
                File.WriteAllText(Path.Combine(folder, "starter.py"), pythonStarter);
            }

            if (javaScriptStarter is not null)
            {
                File.WriteAllText(Path.Combine(folder, "starter.js"), javaScriptStarter);
            }

            if (goalsJson is not null)
            {
                File.WriteAllText(Path.Combine(folder, "goals.json"), goalsJson);
            }

            if (hintsJson is not null)
            {
                File.WriteAllText(Path.Combine(folder, "hints.json"), hintsJson);
            }

            return GameRules.Load(folder);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
