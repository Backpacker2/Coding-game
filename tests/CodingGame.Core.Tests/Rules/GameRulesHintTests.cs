using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using Xunit;

namespace CodingGame.Core.Tests.Rules;

public class GameRulesHintTests
{
    private const string ValidCropsJson = """[{"name":"CLOVER","ground":"GRASS","growTimeTicks":200,"yieldItems":1,"seed":null,"regrows":true},{"name":"RADISH","ground":"GRASS","growTimeTicks":300,"yieldItems":4,"seed":{"item":"CLOVER","count":2},"regrows":false}]""";

    private const string ValidTicksJson = """{"moveTicks":20,"harvestTicks":20,"plantTicks":20,"ripeTicks":1,"tillTicks":20,"groundTicks":1,"loopIterationTicks":1,"functionCallTicks":1,"spinTicks":10,"assignTicks":1}""";

    private const string ValidFieldJson = """{"width":3,"height":3}""";

    private const string StopHintJson = """{"kind":"tooDeep","text":"Stop the recursion."}""";

    [Fact]
    public void LoadsHintsFromDataFolder()
    {
        GameRules rules = GameRules.Load(Path.Combine(AppContext.BaseDirectory, "data"));

        Assert.Equal(11, rules.Hints.Count);
    }

    [Fact]
    public void HintWithoutDetailContainsHasNoDetailCondition()
    {
        GameRules rules = LoadWith($"[{StopHintJson}]");

        Assert.Equal(new Hint(ScriptErrorKind.TooDeep, null, "Stop the recursion."), rules.Hints[0]);
    }

    [Fact]
    public void HintWithDetailContainsKeepsTheText()
    {
        GameRules rules = LoadWith("""[{"kind":"commandFailed","detailContains":"not enough seed items","text":"Harvest more first."}]""");

        Assert.Equal(new Hint(ScriptErrorKind.CommandFailed, "not enough seed items", "Harvest more first."), rules.Hints[0]);
    }

    [Fact]
    public void NullDetailContainsIsTheSameAsNoDetailContains()
    {
        GameRules rules = LoadWith("""[{"kind":"tooDeep","detailContains":null,"text":"Stop the recursion."}]""");

        Assert.Equal(new Hint(ScriptErrorKind.TooDeep, null, "Stop the recursion."), rules.Hints[0]);
    }

    [Fact]
    public void HintsKeepFileOrder()
    {
        GameRules rules = LoadWith("""[{"kind":"tooDeep","text":"Stop."},{"kind":"syntaxError","text":"Compare."}]""");

        Assert.Equal([ScriptErrorKind.TooDeep, ScriptErrorKind.SyntaxError], rules.Hints.Select(hint => hint.Kind));
    }

    public static TheoryData<string, ScriptErrorKind> HintKindCases => new()
    {
        { "undefinedName", ScriptErrorKind.UndefinedName },
        { "wrongArgument", ScriptErrorKind.WrongArgument },
        { "notUnlocked", ScriptErrorKind.NotUnlocked },
        { "notInGame", ScriptErrorKind.NotInGame },
        { "commandFailed", ScriptErrorKind.CommandFailed },
        { "tooDeep", ScriptErrorKind.TooDeep },
        { "syntaxError", ScriptErrorKind.SyntaxError },
        { "indentationError", ScriptErrorKind.IndentationError },
    };

    [Theory]
    [MemberData(nameof(HintKindCases))]
    public void HintKindIsReadAsCamelCaseName(string kindName, ScriptErrorKind expected)
    {
        GameRules rules = LoadWith($$"""[{"kind":"{{kindName}}","text":"Hint."}]""");

        Assert.Equal(expected, rules.Hints[0].Kind);
    }

    public static TheoryData<string, string> InvalidHintCases => new()
    {
        {
            """[{"kind":"visitTiles","text":"Move."}]""",
            "hints.json: entry 1: unknown kind visitTiles"
        },
        {
            """[{"kind":"tooDeep","text":"Stop."},{"kind":"syntaxError","text":" "}]""",
            "hints.json: entry 2: text is empty"
        },
        {
            """[{"kind":"syntaxError","text":"Compare.","detailContains":""}]""",
            "hints.json: entry 1: detailContains is empty"
        },
    };

    [Theory]
    [MemberData(nameof(InvalidHintCases))]
    public void InvalidHintIsReported(string hintsJson, string expectedMessage)
    {
        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(hintsJson));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void MissingHintsFileIsReported()
    {
        GameRulesException exception = Assert.Throws<GameRulesException>(() => LoadWith(null));

        Assert.Equal("hints.json: file not found", exception.Message);
    }

    private static GameRules LoadWith(string? hintsJson)
    {
        string folder = Directory.CreateTempSubdirectory().FullName;
        try
        {
            File.WriteAllText(Path.Combine(folder, "crops.json"), ValidCropsJson);
            File.WriteAllText(Path.Combine(folder, "ticks.json"), ValidTicksJson);
            File.WriteAllText(Path.Combine(folder, "field.json"), ValidFieldJson);
            File.WriteAllText(Path.Combine(folder, "unlocks.json"), "[]");
            File.WriteAllText(Path.Combine(folder, "goals.json"), "[]");
            File.WriteAllText(Path.Combine(folder, "starter.py"), "harvest()\n");
            File.WriteAllText(Path.Combine(folder, "starter.js"), "harvest();\n");
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
