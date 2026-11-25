using System.Text.Json;
using System.Text.Json.Serialization;
using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.JavaScript;
using CodingGame.Core.Interpreter.Python;
using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter;

/// <summary>
/// Runs every example folder under <c>tests/examples</c> in Python and in JavaScript.
/// The same example in every language must give the same end state and the same number of ticks,
/// because both are checked against one expected file.
/// </summary>
public class ExampleScriptTests
{
    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();
    private static readonly string _examplesDirectory = Path.Combine(AppContext.BaseDirectory, "examples");
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Lists one row per example folder and language, in name order.
    /// </summary>
    /// <returns>The example name and the language to run it in.</returns>
    public static TheoryData<string, ScriptLanguage> Examples()
    {
        var rows = new TheoryData<string, ScriptLanguage>();
        IEnumerable<string> names = Directory.GetDirectories(_examplesDirectory)
            .Select(path => Path.GetFileName(path))
            .OrderBy(name => name, StringComparer.Ordinal);
        foreach (string name in names)
        {
            rows.Add(name, ScriptLanguage.Python);
            rows.Add(name, ScriptLanguage.JavaScript);
        }

        return rows;
    }

    /// <summary>
    /// Checks that the script of one example, run in one language, ends in the expected state.
    /// </summary>
    /// <param name="name">The name of the example folder.</param>
    /// <param name="language">The language the script is written in.</param>
    [Theory]
    [MemberData(nameof(Examples))]
    public void ExampleGivesTheExpectedResult(string name, ScriptLanguage language)
    {
        string exampleDirectory = Path.Combine(_examplesDirectory, name);
        string scriptFileName = language switch
        {
            ScriptLanguage.Python => "script.py",
            ScriptLanguage.JavaScript => "script.js",
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null),
        };
        string source = File.ReadAllText(Path.Combine(exampleDirectory, scriptFileName));
        string expectedJson = File.ReadAllText(Path.Combine(exampleDirectory, "expected.json"));
        ExpectedResult expected = JsonSerializer.Deserialize<ExpectedResult>(expectedJson, _jsonOptions)!;

        Crop clover = _rules.Crops.Single(crop => crop.Name == "CLOVER");
        var field = new Field(expected.Width, expected.Height, clover);
        (StepResult result, int ticks) = RunExample(source, language, field);

        Assert.Equal(expected.Status, result.Status);
        Assert.Equal(expected.Ticks, ticks);
        Assert.Equal(new TilePosition(expected.DronePosition.X, expected.DronePosition.Y), field.DronePosition);
        Assert.Equal(expected.Items, field.Items.ToDictionary(item => item.Key, item => item.Value));
        foreach (ExpectedTile tile in expected.Tiles)
        {
            Tile actual = field.GetTile(tile.X, tile.Y);
            Assert.Equal(tile.Ground, actual.Ground);
            Assert.Equal(tile.Crop, actual.Crop?.Name);
        }

        if (expected.Error is null)
        {
            Assert.Null(result.Error);
        }
        else
        {
            Assert.NotNull(result.Error);
            Assert.Equal(expected.Error.Kind, result.Error.Kind);
            Assert.Equal(expected.Error.Line, result.Error.Line);
        }
    }

    private static (StepResult Result, int Ticks) RunExample(string source, ScriptLanguage language, Field field)
    {
        ParseResult parsed = language switch
        {
            ScriptLanguage.Python => PythonFrontEnd.Parse(source),
            ScriptLanguage.JavaScript => JavaScriptFrontEnd.Parse(source),
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null),
        };
        if (parsed.Error is not null)
        {
            return (StepResult.Failed(parsed.Error), 0);
        }

        var evaluator = new Evaluator(parsed.Program!, new DroneApi(_rules, field));
        StepResult result = EvaluatorTestSupport.RunToEnd(evaluator);
        return (result, evaluator.TicksUsed);
    }

    private sealed record ExpectedResult(
        int Width,
        int Height,
        StepStatus Status,
        int Ticks,
        ExpectedPosition DronePosition,
        Dictionary<string, int> Items,
        ExpectedTile[] Tiles,
        ExpectedError? Error);

    private sealed record ExpectedPosition(int X, int Y);

    private sealed record ExpectedTile(int X, int Y, Ground Ground, string? Crop);

    private sealed record ExpectedError(ScriptErrorKind Kind, int Line);
}
