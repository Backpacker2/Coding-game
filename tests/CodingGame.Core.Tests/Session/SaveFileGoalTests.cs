using System.Text.Json;
using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using CodingGame.Core.Tests.Interpreter;
using Xunit;

namespace CodingGame.Core.Tests.Session;

public class SaveFileGoalTests : IDisposable
{
    private const string GrassTile = """{"ground":"GRASS","crop":null,"ageTicks":0}""";

    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public SaveFileGoalTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
        GC.SuppressFinalize(this);
    }

    public static TheoryData<string, string> BadGoalCases => new()
    {
        {
            VersionFourSave("""["NOPE"]"""),
            "save.json: unknown goal id NOPE"
        },
        {
            VersionFourSave("""["BUY_UNLOCK","BUY_UNLOCK"]"""),
            "save.json: duplicate goal id BUY_UNLOCK"
        },
        {
            VersionFourSave(null),
            "save.json: doneGoals is missing"
        },
    };

    [Fact]
    public void DoneGoalsRoundTripInGoalsOrder()
    {
        string path = Path.Combine(_folder, "save.json");
        var progress = new GameProgress(_rules);
        progress.AddItems("CLOVER", 10);
        progress.Buy(UnlockNamed("FOR_LOOP"));
        SaveFile.Save(path, new SavedGame(progress, ScriptLanguage.Python, new ScriptBook(_rules)));

        SavedGame loaded = SaveFile.Load(path, _rules);

        Assert.Equal(["BUY_UNLOCK"], DoneGoalIdsInFile(path));
        Assert.True(loaded.Progress.IsDone(GoalNamed("BUY_UNLOCK")));
        Assert.False(loaded.Progress.IsDone(GoalNamed("FINISH_RUN")));
    }

    [Fact]
    public void VersionFourFileRestoresDoneGoals()
    {
        SavedGame saved = SaveFile.Load(WriteSave(VersionFourSave("""["FINISH_RUN","BUY_UNLOCK"]""")), _rules);

        Assert.True(saved.Progress.IsDone(GoalNamed("FINISH_RUN")));
        Assert.True(saved.Progress.IsDone(GoalNamed("BUY_UNLOCK")));
        Assert.Equal("HAVE_CLOVER", saved.Progress.CurrentGoal?.Id);
    }

    [Fact]
    public void VersionThreeFileLoadsWithNoGoalDone()
    {
        SavedGame saved = SaveFile.Load(WriteSave(VersionThreeSave()), _rules);

        Assert.All(_rules.Goals, goal => Assert.False(saved.Progress.IsDone(goal)));
        Assert.Equal("FINISH_RUN", saved.Progress.CurrentGoal?.Id);
    }

    [Theory]
    [MemberData(nameof(BadGoalCases))]
    public void BadDoneGoalsThrowWithTheExactMessage(string text, string expectedMessage)
    {
        string path = WriteSave(text);

        SaveException exception = Assert.Throws<SaveException>(() => SaveFile.Load(path, _rules));

        Assert.Equal(expectedMessage, exception.Message);
    }

    private static string VersionFourSave(string? doneGoalsArray)
    {
        string doneGoalsProperty = doneGoalsArray is null ? string.Empty : $"\"doneGoals\":{doneGoalsArray},";
        return $$"""{"version":4,"items":{},"boughtUnlocks":[],{{doneGoalsProperty}}"language":"python","scripts":[{"name":"main","python":"a","javaScript":"b"}],"selectedScript":"main","field":{{GrassField()}}}""";
    }

    private static string VersionThreeSave()
    {
        return $$"""{"version":3,"items":{},"boughtUnlocks":[],"language":"python","scripts":[{"name":"main","python":"a","javaScript":"b"}],"selectedScript":"main","field":{{GrassField()}}}""";
    }

    private static string GrassField()
    {
        string tiles = string.Join(",", Enumerable.Repeat(GrassTile, 9));
        return $$"""{"drone":{"x":0,"y":0},"tiles":[{{tiles}}]}""";
    }

    private static Goal GoalNamed(string id)
    {
        return _rules.Goals.Single(goal => goal.Id == id);
    }

    private static Unlock UnlockNamed(string id)
    {
        return _rules.Unlocks.Single(unlock => unlock.Id == id);
    }

    private static List<string> DoneGoalIdsInFile(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("doneGoals").EnumerateArray()
            .Select(element => element.GetString() ?? string.Empty)
            .ToList();
    }

    private string WriteSave(string text)
    {
        string path = Path.Combine(_folder, "save.json");
        File.WriteAllText(path, text);
        return path;
    }
}
