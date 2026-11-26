using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;

namespace CodingGame.Core.Session;

public static partial class SaveFile
{
    private sealed record SaveDto(
        int Version,
        IReadOnlyDictionary<string, int> Items,
        IReadOnlyList<string> BoughtUnlocks,
        ScriptLanguage Language,
        IReadOnlyList<ScriptDto> Scripts,
        string SelectedScript,
        FieldDto? Field = null,
        IReadOnlyList<string>? DoneGoals = null,
        IReadOnlyDictionary<string, int>? BestRates = null);

    private sealed record VersionThreeSaveDto(
        int Version,
        IReadOnlyDictionary<string, int> Items,
        IReadOnlyList<string> BoughtUnlocks,
        ScriptLanguage Language,
        IReadOnlyList<ScriptDto> Scripts,
        string SelectedScript,
        FieldDto? Field = null);

    private sealed record ScriptDto(string Name, string? Python, string? JavaScript);

    private sealed record LegacySaveDto(
        int Version,
        IReadOnlyDictionary<string, int> Items,
        IReadOnlyList<string> BoughtUnlocks,
        ScriptLanguage Language,
        LegacyScriptsDto Scripts,
        FieldDto? Field = null);

    private sealed record LegacyScriptsDto(string? Python, string? JavaScript);

    private sealed record FieldDto(DroneDto Drone, IReadOnlyList<TileDto> Tiles);

    private sealed record DroneDto(int X, int Y);

    private sealed record TileDto(Ground Ground, string? Crop, int AgeTicks);
}
