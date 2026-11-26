using System.Text.Json;
using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
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
    public void SavesProgressAndScriptsAsVersionOneJson()
    {
        string path = Path.Combine(_folder, "save.json");
        SavedGame game = GameWithForLoopAndBiggerField();

        SaveFile.Save(path, game);

        string expected = """
            {
              "version": 1,
              "items": {
                "CLOVER": 12,
                "RADISH": 3
              },
              "boughtUnlocks": [
                "FOR_LOOP",
                "BIGGER_FIELD"
              ],
              "language": "python",
              "scripts": {
                "python": "move(EAST)\n",
                "javaScript": null
              }
            }

            """;
        Assert.Equal(expected.ReplaceLineEndings("\n"), File.ReadAllText(path));
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

        Assert.Contains("\"version\": 1", File.ReadAllText(path));
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
