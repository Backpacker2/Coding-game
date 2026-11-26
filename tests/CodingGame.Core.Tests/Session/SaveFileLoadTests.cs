using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using CodingGame.Core.Simulation;
using CodingGame.Core.Tests.Interpreter;
using Xunit;

namespace CodingGame.Core.Tests.Session;

public class SaveFileLoadTests : IDisposable
{
    private const string MinimalSave = """{"version":1,"items":{},"boughtUnlocks":[],"language":"python","scripts":{"python":null,"javaScript":null}}""";
    private const string GrassTile = """{"ground":"GRASS","crop":null,"ageTicks":0}""";
    private const string DroneAtOrigin = """{"x":0,"y":0}""";

    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public SaveFileLoadTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
        GC.SuppressFinalize(this);
    }

    public static TheoryData<string, string> BadVersionThreeScriptCases => new()
    {
        {
            VersionThreeSave("[]", "main"),
            "save.json: at least one script is needed"
        },
        {
            VersionThreeSave("""[{"name":"","python":"a","javaScript":"b"}]""", ""),
            "save.json: a name needs 1 to 20 characters"
        },
        {
            VersionThreeSave("""[{"name":"main","python":"a","javaScript":"b"},{"name":"Main","python":"c","javaScript":"d"}]""", "main"),
            "save.json: a script with this name already exists"
        },
        {
            VersionThreeSave("""[{"name":"main","python":"a","javaScript":"b"}]""", "nope"),
            "save.json: no script named nope"
        },
        {
            VersionThreeSave("""[{"name":"main","python":null,"javaScript":"b"}]""", "main"),
            "save.json: script main: python is null"
        },
        {
            VersionThreeSave("""[{"name":"main","python":"a","javaScript":null}]""", "main"),
            "save.json: script main: javaScript is null"
        },
    };

    [Fact]
    public void MissingFileIsANewPlayer()
    {
        string path = Path.Combine(_folder, "save.json");

        SavedGame saved = SaveFile.Load(path, _rules);

        Assert.Empty(saved.Progress.Items);
        Assert.All(_rules.Unlocks, unlock => Assert.False(saved.Progress.IsBought(unlock)));
        Assert.Equal(ScriptLanguage.Python, saved.Language);
        Assert.Equal(ScriptBook.FirstScriptName, Assert.Single(saved.Scripts.Names));
        Assert.Equal(_rules.StarterText(ScriptLanguage.Python), saved.Scripts.Text(ScriptBook.FirstScriptName, ScriptLanguage.Python));
    }

    [Fact]
    public void ValidFileRestoresEverything()
    {
        string path = WriteSave("""
            {
              "version": 1,
              "items": { "CLOVER": 12, "RADISH": 3 },
              "boughtUnlocks": [ "FOR_LOOP", "BIGGER_FIELD" ],
              "language": "javaScript",
              "scripts": { "python": "move(EAST)\n", "javaScript": "move(WEST)\n" }
            }
            """);

        SavedGame saved = SaveFile.Load(path, _rules);

        Assert.Equal(12, saved.Progress.Items["CLOVER"]);
        Assert.Equal(3, saved.Progress.Items["RADISH"]);
        Assert.True(saved.Progress.IsBought(UnlockNamed("FOR_LOOP")));
        Assert.True(saved.Progress.IsBought(UnlockNamed("BIGGER_FIELD")));
        Assert.Equal(new FieldSize(5, 5), saved.Progress.FieldSize);
        Assert.Equal(ScriptLanguage.JavaScript, saved.Language);
        Assert.Equal("move(EAST)\n", saved.Scripts.Text("main", ScriptLanguage.Python));
        Assert.Equal("move(WEST)\n", saved.Scripts.Text("main", ScriptLanguage.JavaScript));
    }

    [Fact]
    public void VersionOneTextThatIsNullBecomesTheStarterText()
    {
        SavedGame saved = SaveFile.Load(WriteSave(MinimalSave), _rules);

        Assert.Equal(_rules.StarterText(ScriptLanguage.Python), saved.Scripts.Text("main", ScriptLanguage.Python));
        Assert.Equal(_rules.StarterText(ScriptLanguage.JavaScript), saved.Scripts.Text("main", ScriptLanguage.JavaScript));
    }

    [Fact]
    public void VersionThreeKeepsTheOrderAndTheSelectedScript()
    {
        string path = WriteSave(VersionThreeSave(
            """[{"name":"main","python":"a","javaScript":"b"},{"name":"loop","python":"c","javaScript":"d"}]""",
            "loop"));

        SavedGame saved = SaveFile.Load(path, _rules);

        Assert.Equal(["main", "loop"], saved.Scripts.Names);
        Assert.Equal("loop", saved.Scripts.Selected);
        Assert.Equal("c", saved.Scripts.Text("loop", ScriptLanguage.Python));
        Assert.Equal("d", saved.Scripts.Text("loop", ScriptLanguage.JavaScript));
    }

    [Fact]
    public void SavedFileLoadsAndSavesToTheSameBytes()
    {
        string first = Path.Combine(_folder, "first.json");
        string second = Path.Combine(_folder, "second.json");
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 52);
        progress.AddItems("RADISH", 3);
        progress.Buy(UnlockNamed("FOR_LOOP"));
        progress.Buy(UnlockNamed("BIGGER_FIELD"));
        var scripts = new ScriptBook(_rules);
        scripts.SetText("main", ScriptLanguage.Python, "move(EAST)\n");
        SaveFile.Save(first, new SavedGame(progress, ScriptLanguage.JavaScript, scripts));

        SavedGame loaded = SaveFile.Load(first, _rules);
        SaveFile.Save(second, loaded);

        Assert.Equal(File.ReadAllBytes(first), File.ReadAllBytes(second));
        Assert.Equal(ScriptLanguage.JavaScript, loaded.Language);
        Assert.Equal(progress.Items.OrderBy(item => item.Key), loaded.Progress.Items.OrderBy(item => item.Key));
        Assert.Equal(BoughtIds(progress), BoughtIds(loaded.Progress));
    }

    [Fact]
    public void SavedScriptsRoundTripInOrderAndSelection()
    {
        string path = Path.Combine(_folder, "save.json");
        var scripts = new ScriptBook(_rules);
        scripts.Add();
        scripts.Rename("script 2", "planting");
        scripts.SetText("planting", ScriptLanguage.JavaScript, "harvest();\n");
        scripts.Add();
        scripts.Select("planting");
        SaveFile.Save(path, new SavedGame(new GameProgress(_rules), ScriptLanguage.Python, scripts));

        SavedGame loaded = SaveFile.Load(path, _rules);

        Assert.Equal(["main", "planting", "script 2"], loaded.Scripts.Names);
        Assert.Equal("planting", loaded.Scripts.Selected);
        Assert.Equal("harvest();\n", loaded.Scripts.Text("planting", ScriptLanguage.JavaScript));
    }

    [Theory]
    [InlineData("""{"version":5,"items":{},"boughtUnlocks":[],"language":"python","scripts":{"python":null,"javaScript":null}}""", "save.json: version 5 is not supported, this game reads versions 1 to 4")]
    [InlineData("""{"items":{}}""", "save.json: version is missing")]
    [InlineData("", "save.json: file is not a JSON object")]
    [InlineData("[]", "save.json: file is not a JSON object")]
    [InlineData("""{"version":1,"items":{"TURNIP":1},"boughtUnlocks":[],"language":"python","scripts":{"python":null,"javaScript":null}}""", "save.json: unknown item TURNIP")]
    [InlineData("""{"version":1,"items":{"CLOVER":0},"boughtUnlocks":[],"language":"python","scripts":{"python":null,"javaScript":null}}""", "save.json: CLOVER: count must be at least 1, was 0")]
    [InlineData("""{"version":1,"items":{},"boughtUnlocks":["NOPE"],"language":"python","scripts":{"python":null,"javaScript":null}}""", "save.json: unknown unlock id NOPE")]
    [InlineData("""{"version":1,"items":{},"boughtUnlocks":["FOR_LOOP","FOR_LOOP"],"language":"python","scripts":{"python":null,"javaScript":null}}""", "save.json: duplicate unlock id FOR_LOOP")]
    public void BadSaveThrowsWithTheExactMessage(string text, string expectedMessage)
    {
        string path = WriteSave(text);

        SaveException exception = Assert.Throws<SaveException>(() => SaveFile.Load(path, _rules));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void BrokenJsonThrowsWithTheFileName()
    {
        string path = WriteSave("not json");

        SaveException exception = Assert.Throws<SaveException>(() => SaveFile.Load(path, _rules));

        Assert.StartsWith("save.json: ", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"version":1,"items":{},"boughtUnlocks":[],"language":"python","scripts":{"python":null,"javaScript":null},"extra":1}""")]
    [InlineData("""{"version":1,"items":{},"boughtUnlocks":[],"language":"python"}""")]
    [InlineData("""{"version":1,"items":{},"boughtUnlocks":[],"language":"cobol","scripts":{"python":null,"javaScript":null}}""")]
    [InlineData("""{"version":3,"items":{},"boughtUnlocks":[],"language":"python","scripts":{"python":null,"javaScript":null},"selectedScript":"main"}""")]
    public void WrongShapeThrowsSaveException(string text)
    {
        string path = WriteSave(text);

        Assert.Throws<SaveException>(() => SaveFile.Load(path, _rules));
    }

    [Theory]
    [MemberData(nameof(BadVersionThreeScriptCases))]
    public void BadVersionThreeScriptsThrowWithTheExactMessage(string text, string expectedMessage)
    {
        string path = WriteSave(text);

        SaveException exception = Assert.Throws<SaveException>(() => SaveFile.Load(path, _rules));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void FailedLoadLeavesTheFileUnchanged()
    {
        string path = WriteSave("garbage");

        Assert.Throws<SaveException>(() => SaveFile.Load(path, _rules));

        Assert.Equal("garbage", File.ReadAllText(path));
    }

    [Fact]
    public void VersionOneFileLoadsWithAFreshField()
    {
        SavedGame saved = SaveFile.Load(WriteSave(MinimalSave), _rules);

        Assert.Equal(new TilePosition(0, 0), saved.Progress.Field.DronePosition);
        Assert.Equal(new Tile(Ground.Grass, CloverCrop(), 200), saved.Progress.Field.GetTile(2, 2));
    }

    [Fact]
    public void SavedFieldRoundTripsPlantedTileAndDrone()
    {
        string path = Path.Combine(_folder, "save.json");
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 2);
        progress.Field.Till();
        progress.Field.Till();
        progress.Field.Plant(RadishCrop());
        progress.Field.Advance(50);
        progress.Field.Move(Direction.East);
        SaveFile.Save(path, new SavedGame(progress, ScriptLanguage.Python, new ScriptBook(_rules)));

        SavedGame loaded = SaveFile.Load(path, _rules);

        Assert.Equal(new Tile(Ground.Grass, RadishCrop(), 50), loaded.Progress.Field.GetTile(0, 0));
        Assert.Equal(new TilePosition(1, 0), loaded.Progress.Field.DronePosition);
    }

    [Theory]
    [MemberData(nameof(BadFieldCases))]
    public void BadFieldThrowsWithTheExactMessage(string text, string expectedMessage)
    {
        string path = WriteSave(text);

        SaveException exception = Assert.Throws<SaveException>(() => SaveFile.Load(path, _rules));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void UnknownGroundThrowsSaveException()
    {
        string path = WriteSave(VersionTwoSave(FieldJson(DroneAtOrigin, TilesWithFirst("""{"ground":"LAVA","crop":null,"ageTicks":0}"""))));

        SaveException exception = Assert.Throws<SaveException>(() => SaveFile.Load(path, _rules));

        Assert.StartsWith("save.json: ", exception.Message, StringComparison.Ordinal);
    }

    public static TheoryData<string, string> BadFieldCases => new()
    {
        {
            """{"version":2,"items":{},"boughtUnlocks":[],"language":"python","scripts":{"python":null,"javaScript":null}}""",
            "save.json: field is missing"
        },
        {
            VersionTwoSave(FieldJson(DroneAtOrigin, Tiles(8, GrassTile))),
            "save.json: field has 8 tiles, expected 9 for a 3x3 field"
        },
        {
            VersionTwoSave(FieldJson(DroneAtOrigin, TilesWithFirst("""{"ground":"GRASS","crop":"TURNIP","ageTicks":0}"""))),
            "save.json: tile (0, 0): unknown crop TURNIP"
        },
        {
            VersionTwoSave(FieldJson(DroneAtOrigin, TilesWithFirst("""{"ground":"GRASS","crop":"CLOVER","ageTicks":-1}"""))),
            "save.json: tile (0, 0): age must be from 0 to 200, was -1"
        },
        {
            VersionTwoSave(FieldJson(DroneAtOrigin, TilesWithFirst("""{"ground":"GRASS","crop":"CLOVER","ageTicks":201}"""))),
            "save.json: tile (0, 0): age must be from 0 to 200, was 201"
        },
        {
            VersionTwoSave(FieldJson(DroneAtOrigin, TilesWithFirst("""{"ground":"GRASS","crop":null,"ageTicks":5}"""))),
            "save.json: tile (0, 0): an empty tile has age 0, was 5"
        },
        {
            VersionTwoSave(FieldJson(DroneAtOrigin, TilesWithFirst("""{"ground":"SOIL","crop":"CLOVER","ageTicks":0}"""))),
            "save.json: tile (0, 0): CLOVER grows on Grass, not Soil"
        },
        {
            VersionTwoSave(FieldJson("""{"x":3,"y":0}""", Tiles(9, GrassTile))),
            "save.json: drone (3, 0) is outside the 3x3 field"
        },
    };

    private static string VersionTwoSave(string fieldJson)
    {
        return $$"""{"version":2,"items":{},"boughtUnlocks":[],"language":"python","scripts":{"python":null,"javaScript":null},"field":{{fieldJson}}}""";
    }

    private static string VersionThreeSave(string scriptsJson, string selectedScript)
    {
        return $$"""{"version":3,"items":{},"boughtUnlocks":[],"language":"python","scripts":{{scriptsJson}},"selectedScript":"{{selectedScript}}","field":{{FieldJson(DroneAtOrigin, Tiles(9, GrassTile))}}}""";
    }

    private static string FieldJson(string drone, string tiles)
    {
        return $$"""{"drone":{{drone}},"tiles":[{{tiles}}]}""";
    }

    private static string Tiles(int count, string tile)
    {
        return string.Join(",", Enumerable.Repeat(tile, count));
    }

    private static string TilesWithFirst(string firstTile)
    {
        return string.Join(",", Enumerable.Repeat(GrassTile, 8).Prepend(firstTile));
    }

    private static Crop CloverCrop()
    {
        return _rules.Crops.Single(crop => crop.Name == "CLOVER");
    }

    private static Crop RadishCrop()
    {
        return _rules.Crops.Single(crop => crop.Name == "RADISH");
    }

    private string WriteSave(string text)
    {
        string path = Path.Combine(_folder, "save.json");
        File.WriteAllText(path, text);
        return path;
    }

    private static Unlock UnlockNamed(string id)
    {
        return _rules.Unlocks.Single(unlock => unlock.Id == id);
    }

    private static List<string> BoughtIds(GameProgress progress)
    {
        return progress.Rules.Unlocks.Where(progress.IsBought).Select(unlock => unlock.Id).ToList();
    }
}
