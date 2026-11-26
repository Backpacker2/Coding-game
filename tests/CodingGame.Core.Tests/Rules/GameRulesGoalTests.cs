using CodingGame.Core.Rules;
using Xunit;

namespace CodingGame.Core.Tests.Rules;

public class GameRulesGoalTests
{
    private const string ValidCropsJson = """[{"name":"CLOVER","ground":"GRASS","growTimeTicks":200,"yieldItems":1,"seed":null,"regrows":true},{"name":"RADISH","ground":"GRASS","growTimeTicks":300,"yieldItems":4,"seed":{"item":"CLOVER","count":2},"regrows":false}]""";

    private const string ValidTicksJson = """{"moveTicks":20,"harvestTicks":20,"plantTicks":20,"ripeTicks":1,"tillTicks":20,"groundTicks":1,"loopIterationTicks":1,"functionCallTicks":1,"spinTicks":10,"assignTicks":1}""";

    private const string ValidFieldJson = """{"width":3,"height":3}""";

    private const string FinishRunJson = """{"id":"FINISH_RUN","text":"Run the starter script to the end.","hint":"Press Run.","kind":"finishRun"}""";

    private const string HaveCloverJson = """{"id":"HAVE_CLOVER","text":"Have 3 clover.","hint":"harvest() picks the crop under the drone.","kind":"haveItems","item":"CLOVER","count":3}""";

    [Fact]
    public void LoadsFourGoalsInFileOrderFromDataFolder()
    {
        GameRules rules = GameRules.Load(Path.Combine(AppContext.BaseDirectory, "data"));

        Assert.Equal(["FINISH_RUN", "HAVE_CLOVER", "VISIT_ALL_TILES", "BUY_UNLOCK"], rules.Goals.Select(goal => goal.Id));
    }

    [Fact]
    public void GoalWithoutItemHasNoItemAndZeroCount()
    {
        GameRules rules = LoadWith($"[{FinishRunJson}]");

        Assert.Equal(new Goal("FINISH_RUN", "Run the starter script to the end.", "Press Run.", GoalKind.FinishRun, null, 0), rules.Goals[0]);
    }

    [Fact]
    public void HaveItemsGoalKeepsItemAndCount()
    {
        GameRules rules = LoadWith($"[{HaveCloverJson}]");

        Assert.Equal(new Goal("HAVE_CLOVER", "Have 3 clover.", "harvest() picks the crop under the drone.", GoalKind.HaveItems, "CLOVER", 3), rules.Goals[0]);
    }

    [Fact]
    public void MissingGoalsFileIsReported()
    {
        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(null));

        Assert.Equal("goals.json: file not found", exception.Message);
    }

    public static TheoryData<string, string> InvalidGoalCases => new()
    {
        {
            $"[{FinishRunJson},{FinishRunJson}]",
            "goals.json: duplicate goal id FINISH_RUN"
        },
        {
            """[{"id":"FINISH_RUN","text":"","hint":"Press Run.","kind":"finishRun"}]""",
            "goals.json: FINISH_RUN: text is empty"
        },
        {
            """[{"id":"FINISH_RUN","text":"Run.","hint":"","kind":"finishRun"}]""",
            "goals.json: FINISH_RUN: hint is empty"
        },
        {
            """[{"id":"VISIT","text":"Visit.","hint":"Move.","kind":"visitTiles"}]""",
            "goals.json: VISIT: unknown kind visitTiles"
        },
        {
            """[{"id":"HAVE_CLOVER","text":"Have 3 clover.","hint":"Harvest.","kind":"haveItems","count":3}]""",
            "goals.json: HAVE_CLOVER: haveItems needs an item"
        },
        {
            """[{"id":"HAVE_CARROT","text":"Have 3 carrot.","hint":"Harvest.","kind":"haveItems","item":"CARROT","count":3}]""",
            "goals.json: HAVE_CARROT: item CARROT is not a crop"
        },
        {
            """[{"id":"HAVE_CLOVER","text":"Have 3 clover.","hint":"Harvest.","kind":"haveItems","item":"CLOVER"}]""",
            "goals.json: HAVE_CLOVER: haveItems needs a count"
        },
        {
            """[{"id":"HAVE_CLOVER","text":"Have 3 clover.","hint":"Harvest.","kind":"haveItems","item":"CLOVER","count":0}]""",
            "goals.json: HAVE_CLOVER: count must be greater than 0, was 0"
        },
        {
            """[{"id":"FINISH_RUN","text":"Run.","hint":"Press Run.","kind":"finishRun","count":1}]""",
            "goals.json: FINISH_RUN: item and count are only for haveItems"
        },
        {
            """[{"id":"FINISH_RUN","text":"Run.","hint":"Press Run.","kind":"finishRun","item":"CLOVER"}]""",
            "goals.json: FINISH_RUN: item and count are only for haveItems"
        },
    };

    [Theory]
    [MemberData(nameof(InvalidGoalCases))]
    public void InvalidGoalIsReported(string goalsJson, string expectedMessage)
    {
        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(goalsJson));

        Assert.Equal(expectedMessage, exception.Message);
    }

    private static GameRules LoadWith(string? goalsJson)
    {
        string folder = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(folder, "crops.json"), ValidCropsJson);
            File.WriteAllText(Path.Combine(folder, "ticks.json"), ValidTicksJson);
            File.WriteAllText(Path.Combine(folder, "field.json"), ValidFieldJson);
            File.WriteAllText(Path.Combine(folder, "unlocks.json"), "[]");
            File.WriteAllText(Path.Combine(folder, "starter.py"), "harvest()\n");
            File.WriteAllText(Path.Combine(folder, "starter.js"), "harvest();\n");
            if (goalsJson is not null)
            {
                File.WriteAllText(Path.Combine(folder, "goals.json"), goalsJson);
            }
            File.WriteAllText(Path.Combine(folder, "hints.json"), "[]");

            return GameRules.Load(folder);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
