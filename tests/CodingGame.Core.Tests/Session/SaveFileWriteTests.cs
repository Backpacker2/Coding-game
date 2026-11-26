using System.Text.Json;
using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using CodingGame.Core.Simulation;
using CodingGame.Core.Tests.Interpreter;
using Xunit;

namespace CodingGame.Core.Tests.Session;

public class SaveFileWriteTests : IDisposable
{
    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();
    private static readonly string[] _expectedBoughtOrder = ["FOR_LOOP", "FUNCTIONS"];
    private readonly string _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public SaveFileWriteTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void SavesProgressScriptsAndFieldAsVersionTwoJson()
    {
        string path = Path.Combine(_folder, "save.json");
        SavedGame game = GameWithForLoopAndBiggerField();

        SaveFile.Save(path, game);

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        Assert.Equal(2, root.GetProperty("version").GetInt32());
        Assert.Equal(12, root.GetProperty("items").GetProperty("CLOVER").GetInt32());
        string[] bought = root.GetProperty("boughtUnlocks").EnumerateArray()
            .Select(element => element.GetString() ?? string.Empty)
            .ToArray();
        Assert.Equal(["FOR_LOOP", "BIGGER_FIELD"], bought);
        Assert.Equal("move(EAST)\n", root.GetProperty("scripts").GetProperty("python").GetString());
        JsonElement field = root.GetProperty("field");
        Assert.Equal(0, field.GetProperty("drone").GetProperty("x").GetInt32());
        Assert.Equal(25, field.GetProperty("tiles").GetArrayLength());
        JsonElement firstTile = field.GetProperty("tiles")[0];
        Assert.Equal("GRASS", firstTile.GetProperty("ground").GetString());
        Assert.Equal("CLOVER", firstTile.GetProperty("crop").GetString());
        Assert.Equal(200, firstTile.GetProperty("ageTicks").GetInt32());
    }

    [Fact]
    public void FieldIsWrittenRowByRow()
    {
        string path = Path.Combine(_folder, "save.json");
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 2);
        progress.Field.Move(Direction.East);
        progress.Field.Move(Direction.East);
        progress.Field.Move(Direction.South);
        progress.Field.Till();
        progress.Field.Till();
        progress.Field.Plant(_rules.Crops.Single(crop => crop.Name == "RADISH"));

        SaveFile.Save(path, new SavedGame(progress, ScriptLanguage.Python, new SavedScripts(null, null)));

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement tiles = document.RootElement.GetProperty("field").GetProperty("tiles");
        Assert.Equal("RADISH", tiles[5].GetProperty("crop").GetString());
        Assert.Equal(0, tiles[5].GetProperty("ageTicks").GetInt32());
    }

    [Fact]
    public void BoughtUnlocksFollowTheOrderOfUnlocksJson()
    {
        string path = Path.Combine(_folder, "save.json");
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 10);
        progress.AddItems("POTATO", 30);
        progress.Buy(UnlockNamed("FUNCTIONS"));
        progress.Buy(UnlockNamed("FOR_LOOP"));

        SaveFile.Save(path, new SavedGame(progress, ScriptLanguage.Python, new SavedScripts(null, null)));

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        string[] bought = document.RootElement.GetProperty("boughtUnlocks").EnumerateArray()
            .Select(element => element.GetString() ?? string.Empty)
            .ToArray();
        Assert.Equal(_expectedBoughtOrder, bought);
    }

    [Fact]
    public void SavingTheSameStateTwiceGivesIdenticalBytes()
    {
        string first = Path.Combine(_folder, "first.json");
        string second = Path.Combine(_folder, "second.json");
        SavedGame game = GameWithForLoopAndBiggerField();

        SaveFile.Save(first, game);
        SaveFile.Save(second, game);

        Assert.Equal(File.ReadAllBytes(first), File.ReadAllBytes(second));
    }

    [Fact]
    public void SaveReplacesAnExistingFileAndLeavesNoTempFile()
    {
        string path = Path.Combine(_folder, "save.json");
        File.WriteAllText(path, "old");

        SaveFile.Save(path, GameWithForLoopAndBiggerField());

        Assert.Contains("\"version\": 2", File.ReadAllText(path));
        Assert.Empty(Directory.GetFiles(_folder, "*.tmp"));
    }

    [Fact]
    public void ScriptTextRoundTripsAsValidJson()
    {
        string path = Path.Combine(_folder, "save.json");
        const string script = "say(\"hi\")\nprint('é')\n";
        var scripts = new SavedScripts(script, null);

        SaveFile.Save(path, new SavedGame(new GameProgress(_rules), ScriptLanguage.Python, scripts));

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        string? saved = document.RootElement.GetProperty("scripts").GetProperty("python").GetString();
        Assert.Equal(script, saved);
    }

    [Fact]
    public void SetAsideMovesTheFileToBrokenAndReplacesAnOlderBrokenFile()
    {
        string path = Path.Combine(_folder, "save.json");
        File.WriteAllText(path, "first");

        string keptPath = SaveFile.SetAside(path);

        Assert.Equal(path + ".broken", keptPath);
        Assert.False(File.Exists(path));
        Assert.Equal("first", File.ReadAllText(keptPath));

        File.WriteAllText(path, "second");
        SaveFile.SetAside(path);

        Assert.Equal("second", File.ReadAllText(keptPath));
    }

    private static SavedGame GameWithForLoopAndBiggerField()
    {
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 52);
        progress.AddItems("RADISH", 3);
        progress.Buy(UnlockNamed("FOR_LOOP"));
        progress.Buy(UnlockNamed("BIGGER_FIELD"));
        return new SavedGame(progress, ScriptLanguage.Python, new SavedScripts("move(EAST)\n", null));
    }

    private static Unlock UnlockNamed(string id)
    {
        return _rules.Unlocks.Single(unlock => unlock.Id == id);
    }
}
