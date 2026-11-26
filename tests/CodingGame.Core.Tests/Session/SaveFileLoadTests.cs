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

    [Fact]
    public void MissingFileIsANewPlayer()
    {
        string path = Path.Combine(_folder, "save.json");

        SavedGame saved = SaveFile.Load(path, _rules);

        Assert.Empty(saved.Progress.Items);
        Assert.All(_rules.Unlocks, unlock => Assert.False(saved.Progress.IsBought(unlock)));
        Assert.Equal(ScriptLanguage.Python, saved.Language);
        Assert.Null(saved.Scripts.Python);
        Assert.Null(saved.Scripts.JavaScript);
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
        Assert.Equal("move(EAST)\n", saved.Scripts.Python);
        Assert.Equal("move(WEST)\n", saved.Scripts.JavaScript);
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
        var scripts = new SavedScripts("move(EAST)\n", null);
        SaveFile.Save(first, new SavedGame(progress, ScriptLanguage.JavaScript, scripts));

        SavedGame loaded = SaveFile.Load(first, _rules);
        SaveFile.Save(second, loaded);

        Assert.Equal(File.ReadAllBytes(first), File.ReadAllBytes(second));
        Assert.Equal(ScriptLanguage.JavaScript, loaded.Language);
        Assert.Equal(progress.Items.OrderBy(item => item.Key), loaded.Progress.Items.OrderBy(item => item.Key));
        Assert.Equal(BoughtIds(progress), BoughtIds(loaded.Progress));
    }

    [Theory]
    [InlineData("""{"version":2,"items":{},"boughtUnlocks":[],"language":"python","scripts":{"python":null,"javaScript":null}}""", "save.json: version 2 is not supported, this game reads version 1")]
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
    public void WrongShapeThrowsSaveException(string text)
    {
        string path = WriteSave(text);

        Assert.Throws<SaveException>(() => SaveFile.Load(path, _rules));
    }

    [Fact]
    public void FailedLoadLeavesTheFileUnchanged()
    {
        string path = WriteSave("garbage");

        Assert.Throws<SaveException>(() => SaveFile.Load(path, _rules));

        Assert.Equal("garbage", File.ReadAllText(path));
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
