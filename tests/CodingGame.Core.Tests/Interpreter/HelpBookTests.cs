using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.JavaScript;
using CodingGame.Core.Interpreter.Python;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter;

public class HelpBookTests
{
    private const int MaxTicks = 10_000;

    private const string MoveEntry = """{"name": "move", "summary": "Moves the drone.", "python": "move(EAST)", "javascript": "move(EAST);"}""";

    private static readonly string[] _freeNamesAtStart = ["move", "harvest", "spin", "NORTH", "EAST", "SOUTH", "WEST", "CLOVER"];

    [Fact]
    public void NewPlayerSeesOnlyFreeEntries()
    {
        GameRules rules = EvaluatorTestSupport.Rules();
        HelpBook book = HelpBook.Load(DataFolder(), new DroneApi(rules, EvaluatorTestSupport.NewField(rules)));

        IReadOnlyList<HelpEntry> entries = book.EntriesFor(new GameProgress(rules), ScriptLanguage.Python);

        Assert.Equal(_freeNamesAtStart, ReferenceNames(entries));
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

        Assert.Equal(37, entries.Count);
    }

    [Fact]
    public void VariableEntriesAppearOnlyAfterBuyingVariables()
    {
        GameRules rules = EvaluatorTestSupport.Rules();
        HelpBook book = HelpBook.Load(DataFolder(), new DroneApi(rules, EvaluatorTestSupport.NewField(rules)));
        var progress = new GameProgress(rules);
        List<string> beforeVariables = NamesFor(book, progress);

        progress.AddItems("POTATO", 40);
        progress.Buy(UnlockWithId(rules, "VARIABLES"));
        List<string> afterVariables = NamesFor(book, progress);

        Assert.DoesNotContain("variables", beforeVariables);
        Assert.DoesNotContain("+", beforeVariables);
        Assert.DoesNotContain("<", beforeVariables);
        Assert.Contains("variables", afterVariables);
        Assert.Contains("+", afterVariables);
        Assert.Contains("<", afterVariables);
    }

    [Theory]
    [InlineData("variables", ScriptLanguage.Python)]
    [InlineData("variables", ScriptLanguage.JavaScript)]
    [InlineData("+", ScriptLanguage.Python)]
    [InlineData("+", ScriptLanguage.JavaScript)]
    [InlineData("<", ScriptLanguage.Python)]
    [InlineData("<", ScriptLanguage.JavaScript)]
    public void VariableHelpExamplesFinish(string name, ScriptLanguage language)
    {
        GameProgress progress = EvaluatorTestSupport.AllUnlocked(EvaluatorTestSupport.Rules());
        string example = EntriesFor(progress, language).Single(entry => entry.Name == name).Example;
        RunSession session = RunSession.Start(example, language, progress);

        for (int tick = 0; tick < MaxTicks && session.Status == StepStatus.Running; tick++)
        {
            session.StepTick();
        }

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Null(session.ErrorMessage);
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
    public void NewPlayerSeesTheFirstThreeLessonsThenTheNextOne()
    {
        IReadOnlyList<HelpEntry> entries = EntriesFor(new GameProgress(EvaluatorTestSupport.Rules()), ScriptLanguage.Python);

        Assert.Equal(
            [HelpKind.Lesson, HelpKind.Lesson, HelpKind.Lesson, HelpKind.NextLesson],
            entries.Take(4).Select(entry => entry.Kind));
        Assert.Equal(["Your first script", "Comments", "Moving around the field"], LessonNames(entries));
        HelpEntry next = entries.Single(entry => entry.Kind == HelpKind.NextLesson);
        Assert.Equal("Repeating with for", next.Name);
        Assert.StartsWith("Next: unlock \"for loop\" to learn this. ", next.Summary);
    }

    [Fact]
    public void BuyingForLoopShowsLessonsFourAndFiveAndNextSix()
    {
        GameRules rules = EvaluatorTestSupport.Rules();
        var progress = new GameProgress(rules);
        progress.AddItems("CLOVER", 10);
        progress.Buy(UnlockWithId(rules, "FOR_LOOP"));

        IReadOnlyList<HelpEntry> entries = EntriesFor(progress, ScriptLanguage.Python);

        Assert.Equal(
            ["Your first script", "Comments", "Moving around the field", "Repeating with for", "Loops inside loops"],
            LessonNames(entries));
        HelpEntry next = entries.Single(entry => entry.Kind == HelpKind.NextLesson);
        Assert.Equal("Choosing with if", next.Name);
        Assert.StartsWith("Next: unlock \"if / else and ripe()\" to learn this. ", next.Summary);
    }

    [Fact]
    public void EverythingUnlockedShowsEveryLessonAndNoNextOne()
    {
        GameRules rules = EvaluatorTestSupport.Rules();

        IReadOnlyList<HelpEntry> entries = EntriesFor(EvaluatorTestSupport.AllUnlocked(rules), ScriptLanguage.Python);

        Assert.Equal(12, LessonNames(entries).Count);
        Assert.DoesNotContain(entries, entry => entry.Kind == HelpKind.NextLesson);
    }

    [Fact]
    public void CountingLessonIsNextAfterFunctions()
    {
        GameRules rules = EvaluatorTestSupport.Rules();
        var progress = new GameProgress(rules);
        foreach (Unlock unlock in rules.Unlocks.TakeWhile(candidate => candidate.Id != "VARIABLES"))
        {
            progress.AddItems(unlock.Price.Item, unlock.Price.Count);
            progress.Buy(unlock);
        }

        HelpEntry next = EntriesFor(progress, ScriptLanguage.Python).Single(entry => entry.Kind == HelpKind.NextLesson);

        Assert.Equal("Counting with the loop counter", next.Name);
        Assert.StartsWith("Next: unlock \"Variables and counting\" to learn this. ", next.Summary);
    }

    [Fact]
    public void LessonUsesTheTextOfTheLanguage()
    {
        GameRules rules = EvaluatorTestSupport.Rules();
        var progress = new GameProgress(rules);
        progress.AddItems("CLOVER", 10);
        progress.Buy(UnlockWithId(rules, "FOR_LOOP"));

        HelpEntry python = EntriesFor(progress, ScriptLanguage.Python).Single(entry => entry.Name == "Repeating with for");
        HelpEntry javaScript = EntriesFor(progress, ScriptLanguage.JavaScript).Single(entry => entry.Name == "Repeating with for");

        Assert.StartsWith("for i in range(3):", python.Example);
        Assert.StartsWith("for (let i = 0; i < 3; i++)", javaScript.Example);
        Assert.NotEqual(python.Summary, javaScript.Summary);
    }

    [Fact]
    public void LessonsWithoutFileIsReported()
    {
        Assert.Equal("lessons.json: file not found", LessonsLoadErrorFor(null));
    }

    [Fact]
    public void EmptyLessonsFileIsReported()
    {
        Assert.Equal("lessons.json: file is empty", LessonsLoadErrorFor("null"));
    }

    [Fact]
    public void InvalidLessonsJsonIsReported()
    {
        Assert.StartsWith("lessons.json: invalid JSON: ", LessonsLoadErrorFor("[{"));
    }

    [Fact]
    public void UnknownLessonFeatureIsReported()
    {
        string json = "[" + LessonJson("first", feature: "\"fly\"") + "]";

        Assert.Equal("lessons.json: lesson 'first': unknown feature 'fly'", LessonsLoadErrorFor(json));
    }

    [Fact]
    public void DuplicateLessonIdIsReported()
    {
        string json = "[" + LessonJson("first") + "," + LessonJson("first") + "]";

        Assert.Equal("lessons.json: duplicate id 'first'", LessonsLoadErrorFor(json));
    }

    [Fact]
    public void EmptyLessonTextIsReported()
    {
        string json = "[" + LessonJson("first", text: " ") + "]";

        Assert.Equal(
            "lessons.json: lesson 'first': title, python and javascript text and example must not be empty",
            LessonsLoadErrorFor(json));
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
        return ReferenceNames(book.EntriesFor(progress, ScriptLanguage.Python));
    }

    private static List<string> ReferenceNames(IEnumerable<HelpEntry> entries)
    {
        return entries
            .Where(entry => entry.Kind is not (HelpKind.Lesson or HelpKind.NextLesson))
            .Select(entry => entry.Name)
            .ToList();
    }

    private static List<string> LessonNames(IEnumerable<HelpEntry> entries)
    {
        return entries.Where(entry => entry.Kind == HelpKind.Lesson).Select(entry => entry.Name).ToList();
    }

    private static IReadOnlyList<HelpEntry> EntriesFor(GameProgress progress, ScriptLanguage language)
    {
        GameRules rules = EvaluatorTestSupport.Rules();
        HelpBook book = HelpBook.Load(DataFolder(), new DroneApi(rules, EvaluatorTestSupport.NewField(rules)));
        return book.EntriesFor(progress, language);
    }

    private static string LessonJson(string id, string feature = "null", string text = "Text.")
    {
        return $$$"""{"id": "{{{id}}}", "title": "Title", "feature": {{{feature}}}, "python": {"text": "{{{text}}}", "example": "harvest()"}, "javascript": {"text": "{{{text}}}", "example": "harvest();"}}""";
    }

    private static string LessonsLoadErrorFor(string? lessonsJson)
    {
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.Copy(Path.Combine(DataFolder(), "help.json"), Path.Combine(folder, "help.json"));
            if (lessonsJson is not null)
            {
                File.WriteAllText(Path.Combine(folder, "lessons.json"), lessonsJson);
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
