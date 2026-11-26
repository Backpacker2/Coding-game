using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.JavaScript;
using CodingGame.Core.Interpreter.Python;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter;

public class HelpBookTests
{
    private const string MoveEntry = """{"name": "move", "summary": "Moves the drone.", "python": "move(EAST)", "javascript": "move(EAST);"}""";

    private static readonly string[] _freeNamesAtStart = ["move", "harvest", "spin", "NORTH", "EAST", "SOUTH", "WEST", "CLOVER"];

    [Fact]
    public void NewPlayerSeesOnlyFreeEntries()
    {
        GameRules rules = EvaluatorTestSupport.Rules();
        HelpBook book = HelpBook.Load(DataFolder(), new DroneApi(rules, EvaluatorTestSupport.NewField(rules)));

        IReadOnlyList<HelpEntry> entries = book.EntriesFor(new GameProgress(rules), ScriptLanguage.Python);

        Assert.Equal(_freeNamesAtStart, entries.Select(entry => entry.Name));
    }

    [Fact]
    public void ExampleFollowsTheLanguage()
    {
        GameRules rules = EvaluatorTestSupport.Rules();
        HelpBook book = HelpBook.Load(DataFolder(), new DroneApi(rules, EvaluatorTestSupport.NewField(rules)));
        var progress = new GameProgress(rules);

        HelpEntry python = book.EntriesFor(progress, ScriptLanguage.Python).Single(entry => entry.Name == "move");
        HelpEntry javaScript = book.EntriesFor(progress, ScriptLanguage.JavaScript).Single(entry => entry.Name == "move");

        Assert.Equal("move(EAST)", python.Example);
        Assert.Equal("move(EAST);", javaScript.Example);
    }

    [Fact]
    public void BuyingAnUnlockAddsItsFeatures()
    {
        GameRules rules = EvaluatorTestSupport.Rules();
        HelpBook book = HelpBook.Load(DataFolder(), new DroneApi(rules, EvaluatorTestSupport.NewField(rules)));
        var progress = new GameProgress(rules);
        progress.AddItems("CLOVER", 10);
        progress.Buy(UnlockWithId(rules, "FOR_LOOP"));

        List<string> afterForLoop = NamesFor(book, progress);
        progress.AddItems("CLOVER", 40);
        progress.Buy(UnlockWithId(rules, "RADISH"));
        List<string> afterRadish = NamesFor(book, progress);

        Assert.Contains("for", afterForLoop);
        Assert.Contains("plant", afterRadish);
        Assert.Contains("RADISH", afterRadish);
    }

    [Fact]
    public void AFieldSizeUnlockAddsNoEntries()
    {
        GameRules rules = EvaluatorTestSupport.Rules();
        HelpBook book = HelpBook.Load(DataFolder(), new DroneApi(rules, EvaluatorTestSupport.NewField(rules)));
        var progress = new GameProgress(rules);
        progress.AddItems("CLOVER", 30);
        progress.Buy(UnlockWithId(rules, "BIGGER_FIELD"));

        Assert.Equal(_freeNamesAtStart, NamesFor(book, progress));
    }

    [Fact]
    public void EverythingUnlockedShowsEveryEntry()
    {
        GameRules rules = EvaluatorTestSupport.Rules();
        HelpBook book = HelpBook.Load(DataFolder(), new DroneApi(rules, EvaluatorTestSupport.NewField(rules)));

        IReadOnlyList<HelpEntry> entries = book.EntriesFor(EvaluatorTestSupport.AllUnlocked(rules), ScriptLanguage.Python);

        Assert.Equal(21, entries.Count);
    }

    [Fact]
    public void KindsAreDerived()
    {
        GameRules rules = EvaluatorTestSupport.Rules();
        HelpBook book = HelpBook.Load(DataFolder(), new DroneApi(rules, EvaluatorTestSupport.NewField(rules)));
        IReadOnlyList<HelpEntry> entries = book.EntriesFor(EvaluatorTestSupport.AllUnlocked(rules), ScriptLanguage.Python);

        Assert.Equal(HelpKind.Command, entries.Single(entry => entry.Name == "move").Kind);
        Assert.Equal(HelpKind.Constant, entries.Single(entry => entry.Name == "NORTH").Kind);
        Assert.Equal(HelpKind.Keyword, entries.Single(entry => entry.Name == "for").Kind);
    }

    [Fact]
    public void EveryExampleParses()
    {
        GameRules rules = EvaluatorTestSupport.Rules();
        HelpBook book = HelpBook.Load(DataFolder(), new DroneApi(rules, EvaluatorTestSupport.NewField(rules)));
        GameProgress progress = EvaluatorTestSupport.AllUnlocked(rules);

        foreach (HelpEntry entry in book.EntriesFor(progress, ScriptLanguage.Python))
        {
            Assert.Null(PythonFrontEnd.Parse(entry.Example).Error);
        }

        foreach (HelpEntry entry in book.EntriesFor(progress, ScriptLanguage.JavaScript))
        {
            Assert.Null(JavaScriptFrontEnd.Parse(entry.Example).Error);
        }
    }

    [Fact]
    public void LoadSucceedsOnTheGameData()
    {
        GameRules rules = EvaluatorTestSupport.Rules();

        HelpBook book = HelpBook.Load(DataFolder(), new DroneApi(rules, EvaluatorTestSupport.NewField(rules)));

        Assert.NotEmpty(book.EntriesFor(new GameProgress(rules), ScriptLanguage.Python));
    }

    [Fact]
    public void MissingFileIsReported()
    {
        Assert.Equal("help.json: file not found", LoadErrorFor(null));
    }

    [Fact]
    public void UnknownNameIsReported()
    {
        string json = """[{"name": "fly", "summary": "Flies.", "python": "fly()", "javascript": "fly();"}]""";

        Assert.Equal("help.json: unknown name 'fly'", LoadErrorFor(json));
    }

    [Fact]
    public void DuplicateEntryIsReported()
    {
        string json = "[" + MoveEntry + "," + MoveEntry + "]";

        Assert.Equal("help.json: duplicate entry 'move'", LoadErrorFor(json));
    }

    [Fact]
    public void EmptySummaryIsReported()
    {
        string json = """[{"name": "move", "summary": " ", "python": "move(EAST)", "javascript": "move(EAST);"}]""";

        Assert.Equal("help.json: entry 'move': summary, python and javascript must not be empty", LoadErrorFor(json));
    }

    [Fact]
    public void MissingEntryIsReported()
    {
        string json = "[" + MoveEntry + "]";

        Assert.Equal("help.json: no entry for 'harvest'", LoadErrorFor(json));
    }

    private static string DataFolder()
    {
        return Path.Combine(AppContext.BaseDirectory, "data");
    }

    private static Unlock UnlockWithId(GameRules rules, string id)
    {
        return rules.Unlocks.Single(unlock => unlock.Id == id);
    }

    private static List<string> NamesFor(HelpBook book, GameProgress progress)
    {
        return book.EntriesFor(progress, ScriptLanguage.Python).Select(entry => entry.Name).ToList();
    }

    private static string LoadErrorFor(string? helpJson)
    {
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            if (helpJson is not null)
            {
                File.WriteAllText(Path.Combine(folder, "help.json"), helpJson);
            }

            GameRules rules = EvaluatorTestSupport.Rules();
            GameRulesException exception = Assert.Throws<GameRulesException>(
                () => HelpBook.Load(folder, new DroneApi(rules, EvaluatorTestSupport.NewField(rules))));
            return exception.Message;
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
