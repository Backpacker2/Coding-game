using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;

namespace CodingGame.Core.Session;

/// <summary>
/// Writes and reads the save file: one JSON file with the progress, the selected language and the scripts.
/// </summary>
public static class SaveFile
{
    /// <summary>
    /// The save format version that <see cref="Save"/> writes.
    /// </summary>
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private sealed record SaveDto(
        int Version,
        IReadOnlyDictionary<string, int> Items,
        IReadOnlyList<string> BoughtUnlocks,
        ScriptLanguage Language,
        SavedScripts Scripts);

    /// <summary>
    /// Writes a game to a save file. The same game always gives the same bytes: items are sorted by name and
    /// unlocks follow the order of unlocks.json. The file is written to a temporary file first and then moved
    /// over <paramref name="path"/>, so a crash never leaves half a file.
    /// </summary>
    /// <param name="path">The path of the save file. Its folder must exist.</param>
    /// <param name="game">The game to write.</param>
    /// <exception cref="IOException">The file cannot be written, for example when the disk is full.</exception>
    public static void Save(string path, SavedGame game)
    {
        var items = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, int> item in game.Progress.Items)
        {
            items[item.Key] = item.Value;
        }

        List<string> boughtUnlocks = game.Progress.Rules.Unlocks
            .Where(game.Progress.IsBought)
            .Select(unlock => unlock.Id)
            .ToList();
        var dto = new SaveDto(CurrentVersion, items, boughtUnlocks, game.Language, game.Scripts);
        string text = JsonSerializer.Serialize(dto, _jsonOptions) + "\n";

        string temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, text);
        File.Move(temporaryPath, path, overwrite: true);
    }

    /// <summary>
    /// Reads a save file and checks it against the game rules. A missing file is a new player and gives a new game.
    /// </summary>
    /// <param name="path">The path of the save file.</param>
    /// <param name="rules">The game rules that the item names, the unlock ids and the language must match.</param>
    /// <returns>The saved game, or a new game when there is no file.</returns>
    /// <exception cref="SaveException">The file is broken, is from another save version, or breaks a rule such as an unknown unlock id or item.</exception>
    public static SavedGame Load(string path, GameRules rules)
    {
        string fileName = Path.GetFileName(path);
        if (!File.Exists(path))
        {
            return new SavedGame(new GameProgress(rules), ScriptLanguage.Python, new SavedScripts(null, null));
        }

        string text = File.ReadAllText(path);
        CheckVersion(fileName, text);
        SaveDto dto = ReadDto(fileName, text);
        CheckItems(fileName, dto.Items, rules);
        IReadOnlySet<string> boughtIds = CheckBoughtUnlocks(fileName, dto.BoughtUnlocks, rules);
        return new SavedGame(new GameProgress(rules, dto.Items, boughtIds), dto.Language, dto.Scripts);
    }

    /// <summary>
    /// Moves a save file that cannot be read out of the way, to path + ".broken". An older ".broken" file is replaced.
    /// </summary>
    /// <param name="path">The path of the save file to move.</param>
    /// <returns>The new path of the moved file.</returns>
    /// <exception cref="FileNotFoundException">There is no file at <paramref name="path"/>.</exception>
    public static string SetAside(string path)
    {
        string brokenPath = path + ".broken";
        File.Move(path, brokenPath, overwrite: true);
        return brokenPath;
    }

    private static void CheckVersion(string fileName, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new SaveException(fileName, "file is not a JSON object");
        }

        using JsonDocument document = ParseText(fileName, text);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new SaveException(fileName, "file is not a JSON object");
        }

        if (!document.RootElement.TryGetProperty("version", out JsonElement versionElement)
            || !versionElement.TryGetInt32(out int version))
        {
            throw new SaveException(fileName, "version is missing");
        }

        if (version != CurrentVersion)
        {
            throw new SaveException(fileName, $"version {version} is not supported, this game reads version {CurrentVersion}");
        }
    }

    private static JsonDocument ParseText(string fileName, string text)
    {
        try
        {
            return JsonDocument.Parse(text);
        }
        catch (JsonException exception)
        {
            throw new SaveException(fileName, exception.Message);
        }
    }

    private static SaveDto ReadDto(string fileName, string text)
    {
        try
        {
            // The version check has already shown the root is an object, so the result is never null.
            return JsonSerializer.Deserialize<SaveDto>(text, _jsonOptions)!;
        }
        catch (JsonException exception)
        {
            throw new SaveException(fileName, exception.Message);
        }
    }

    private static void CheckItems(string fileName, IReadOnlyDictionary<string, int> items, GameRules rules)
    {
        foreach (KeyValuePair<string, int> item in items)
        {
            if (!rules.Crops.Any(crop => crop.Name == item.Key))
            {
                throw new SaveException(fileName, $"unknown item {item.Key}");
            }

            if (item.Value < 1)
            {
                throw new SaveException(fileName, $"{item.Key}: count must be at least 1, was {item.Value}");
            }
        }
    }

    private static HashSet<string> CheckBoughtUnlocks(string fileName, IReadOnlyList<string> ids, GameRules rules)
    {
        var checkedIds = new HashSet<string>();
        foreach (string id in ids)
        {
            if (!rules.Unlocks.Any(unlock => unlock.Id == id))
            {
                throw new SaveException(fileName, $"unknown unlock id {id}");
            }

            if (!checkedIds.Add(id))
            {
                throw new SaveException(fileName, $"duplicate unlock id {id}");
            }
        }

        return checkedIds;
    }
}
