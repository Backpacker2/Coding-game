using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;

namespace CodingGame.Core.Session;

/// <summary>
/// Writes and reads the save file: one JSON file with the progress, the field, the selected language and the scripts.
/// </summary>
public static partial class SaveFile
{
    /// <summary>
    /// The save format version that <see cref="Save"/> writes. Versions 1 to 4 still load.
    /// </summary>
    public const int CurrentVersion = 5;

    private const int FirstVersionWithField = 2;
    private const int FirstVersionWithScripts = 3;
    private const int FirstVersionWithDoneGoals = 4;
    private const int FirstVersionWithBestRates = 5;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter<Ground>(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false),
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
        },
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Writes a game to a save file. The same game always gives the same bytes: items are sorted by name, unlocks
    /// follow the order of unlocks.json, and tiles are written row by row. The file is written to a temporary file
    /// first and then moved over <paramref name="path"/>, so a crash never leaves half a file.
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

        var bestRates = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, int> best in game.Progress.BestRates)
        {
            bestRates[best.Key] = best.Value;
        }

        List<string> boughtUnlocks = game.Progress.Rules.Unlocks
            .Where(game.Progress.IsBought)
            .Select(unlock => unlock.Id)
            .ToList();
        List<string> doneGoals = game.Progress.Rules.Goals
            .Where(game.Progress.IsDone)
            .Select(goal => goal.Id)
            .ToList();
        List<ScriptDto> scripts = game.Scripts.Names
            .Select(name => new ScriptDto(name, game.Scripts.Text(name, ScriptLanguage.Python), game.Scripts.Text(name, ScriptLanguage.JavaScript)))
            .ToList();
        var dto = new SaveDto(
            CurrentVersion,
            items,
            boughtUnlocks,
            game.Language,
            scripts,
            game.Scripts.Selected,
            ToFieldDto(game.Progress.Field),
            doneGoals,
            bestRates);
        string text = JsonSerializer.Serialize(dto, _jsonOptions) + "\n";

        string temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, text);
        File.Move(temporaryPath, path, overwrite: true);
    }

    /// <summary>
    /// Reads a save file and checks it against the game rules. A missing file is a new player and gives a new game.
    /// A version 1 file has no field and gives a fresh start field. A version 1 or 2 file has one script text per
    /// language, and becomes one script named main; a text that is null becomes the starter text. A version 1 to 3
    /// file has no goals done, and a version 1 to 4 file has no best rates.
    /// </summary>
    /// <param name="path">The path of the save file.</param>
    /// <param name="rules">The game rules that the item names, the unlock ids, the goal ids, the crops, the language and the starter texts must match.</param>
    /// <returns>The saved game, or a new game when there is no file.</returns>
    /// <exception cref="SaveException">The file is broken, is from another save version, or breaks a rule such as an unknown unlock or goal id, an item or crop that is unknown, a tile that does not fit the field, a drone outside the field, a missing doneGoals or bestRates list, a best rate that is for an unknown item or is below 1, or a script that has no name, a duplicate name, no texts or a selected script that does not exist.</exception>
    public static SavedGame Load(string path, GameRules rules)
    {
        string fileName = Path.GetFileName(path);
        if (!File.Exists(path))
        {
            return new SavedGame(new GameProgress(rules), ScriptLanguage.Python, new ScriptBook(rules));
        }

        string text = File.ReadAllText(path);
        int version = CheckVersion(fileName, text);
        if (version >= FirstVersionWithDoneGoals)
        {
            SaveDto dto = ReadDto<SaveDto>(fileName, text);
            IReadOnlyList<string> doneGoals = dto.DoneGoals ?? throw new SaveException(fileName, "doneGoals is missing");
            IReadOnlyDictionary<string, int> bestRates = ReadBestRates(fileName, rules, version, dto.BestRates);
            GameProgress progress = ReadProgress(fileName, rules, version, dto.Items, dto.BoughtUnlocks, doneGoals, bestRates, dto.Field);
            return new SavedGame(progress, dto.Language, ReadScripts(fileName, rules, dto.Scripts, dto.SelectedScript));
        }

        if (version == FirstVersionWithScripts)
        {
            VersionThreeSaveDto three = ReadDto<VersionThreeSaveDto>(fileName, text);
            GameProgress threeProgress = ReadProgress(fileName, rules, version, three.Items, three.BoughtUnlocks, [], new Dictionary<string, int>(), three.Field);
            return new SavedGame(threeProgress, three.Language, ReadScripts(fileName, rules, three.Scripts, three.SelectedScript));
        }

        LegacySaveDto legacy = ReadDto<LegacySaveDto>(fileName, text);
        GameProgress legacyProgress = ReadProgress(fileName, rules, version, legacy.Items, legacy.BoughtUnlocks, [], new Dictionary<string, int>(), legacy.Field);
        return new SavedGame(legacyProgress, legacy.Language, ReadLegacyScripts(rules, legacy.Scripts));
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

    private static FieldDto ToFieldDto(Field field)
    {
        var tiles = new List<TileDto>();
        for (int y = 0; y < field.Height; y++)
        {
            for (int x = 0; x < field.Width; x++)
            {
                Tile tile = field.GetTile(x, y);
                tiles.Add(new TileDto(tile.Ground, tile.Crop?.Name, tile.CropAgeTicks));
            }
        }

        return new FieldDto(new DroneDto(field.DronePosition.X, field.DronePosition.Y), tiles);
    }

    private static GameProgress ReadProgress(
        string fileName,
        GameRules rules,
        int version,
        IReadOnlyDictionary<string, int> items,
        IReadOnlyList<string> boughtUnlocks,
        IReadOnlyList<string> doneGoals,
        IReadOnlyDictionary<string, int> bestRates,
        FieldDto? field)
    {
        CheckItems(fileName, items, rules);
        IReadOnlySet<string> boughtIds = CheckBoughtUnlocks(fileName, boughtUnlocks, rules);
        IReadOnlySet<string> doneGoalIds = CheckDoneGoals(fileName, doneGoals, rules);

        // Only the field size is needed to check the tiles. The progress built here is replaced by the one below.
        FieldSize size = new GameProgress(rules, items, boughtIds, doneGoalIds, bestRates, null, default).FieldSize;
        Tile[,]? tiles = null;
        TilePosition dronePosition = default;
        if (field is { } fieldDto)
        {
            (tiles, dronePosition) = ReadField(fileName, fieldDto, rules, size);
        }
        else if (version >= FirstVersionWithField)
        {
            throw new SaveException(fileName, "field is missing");
        }

        return new GameProgress(rules, items, boughtIds, doneGoalIds, bestRates, tiles, dronePosition);
    }

    private static int CheckVersion(string fileName, string text)
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

        if (version is < 1 or > CurrentVersion)
        {
            throw new SaveException(fileName, $"version {version} is not supported, this game reads versions 1 to {CurrentVersion}");
        }

        return version;
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

    private static T ReadDto<T>(string fileName, string text) where T : class
    {
        try
        {
            // The version check has already shown the root is an object, so the result is never null.
            return JsonSerializer.Deserialize<T>(text, _jsonOptions)!;
        }
        catch (JsonException exception)
        {
            throw new SaveException(fileName, exception.Message);
        }
    }
}
