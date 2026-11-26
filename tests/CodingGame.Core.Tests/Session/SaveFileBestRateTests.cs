using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using CodingGame.Core.Tests.Interpreter;
using Xunit;

namespace CodingGame.Core.Tests.Session;

public class SaveFileBestRateTests : IDisposable
{
    private const string GrassTile = """{"ground":"GRASS","crop":null,"ageTicks":0}""";
    private const string VersionOneSave = """{"version":1,"items":{},"boughtUnlocks":[],"language":"python","scripts":{"python":null,"javaScript":null}}""";

    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public SaveFileBestRateTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
        GC.SuppressFinalize(this);
    }

    public static TheoryData<string, string> BadBestRateCases => new()
    {
        {
            VersionFiveSave("""{"TURNIP":5}"""),
            "save.json: bestRates: unknown item TURNIP"
        },
        {
            VersionFiveSave("""{"CLOVER":0}"""),
            "save.json: bestRates: CLOVER: best rate must be at least 1, was 0"
        },
        {
            VersionFiveSave(null),
            "save.json: bestRates is missing"
        },
    };

    [Fact]
    public void VersionFiveFileRestoresBestRates()
    {
        SavedGame saved = SaveFile.Load(WriteSave(VersionFiveSave("""{"CLOVER":15}""")), _rules);

        Assert.Equal(15, saved.Progress.BestRates["CLOVER"]);
    }

    [Fact]
    public void OlderFilesLoadWithNoBestRates()
    {
        SavedGame versionOne = SaveFile.Load(WriteSave(VersionOneSave), _rules);
        SavedGame versionFour = SaveFile.Load(WriteSave(VersionFourSave()), _rules);

        Assert.Empty(versionOne.Progress.BestRates);
        Assert.Empty(versionFour.Progress.BestRates);
    }

    [Theory]
    [MemberData(nameof(BadBestRateCases))]
    public void BadBestRatesThrowWithTheExactMessage(string text, string expectedMessage)
    {
        string path = WriteSave(text);

        SaveException exception = Assert.Throws<SaveException>(() => SaveFile.Load(path, _rules));

        Assert.Equal(expectedMessage, exception.Message);
    }

    private static string VersionFourSave()
    {
        return $$"""{"version":4,"items":{},"boughtUnlocks":[],"doneGoals":[],"language":"python","scripts":[{"name":"main","python":"a","javaScript":"b"}],"selectedScript":"main","field":{{GrassField()}}}""";
    }

    private static string VersionFiveSave(string? bestRatesJson)
    {
        string bestRatesProperty = bestRatesJson is null ? string.Empty : $"\"bestRates\":{bestRatesJson},";
        return $$"""{"version":5,"items":{},"boughtUnlocks":[],"doneGoals":[],{{bestRatesProperty}}"language":"python","scripts":[{"name":"main","python":"a","javaScript":"b"}],"selectedScript":"main","field":{{GrassField()}}}""";
    }

    private static string GrassField()
    {
        string tiles = string.Join(",", Enumerable.Repeat(GrassTile, 9));
        return $$"""{"drone":{"x":0,"y":0},"tiles":[{{tiles}}]}""";
    }

    private string WriteSave(string text)
    {
        string path = Path.Combine(_folder, "save.json");
        File.WriteAllText(path, text);
        return path;
    }
}
