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
public static class SaveFile
{
    /// <summary>
    /// The save format version that <see cref="Save"/> writes. Version 1 files, which have no field, still load.
    /// </summary>
    public const int CurrentVersion = 2;

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

    private sealed record SaveDto(
        int Version,
        IReadOnlyDictionary<string, int> Items,
        IReadOnlyList<string> BoughtUnlocks,
        ScriptLanguage Language,
        SavedScripts Scripts,
        FieldDto? Field = null);

    private sealed record FieldDto(DroneDto Drone, IReadOnlyList<TileDto> Tiles);

    private sealed record DroneDto(int X, int Y);

    private sealed record TileDto(Ground Ground, string? Crop, int AgeTicks);

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

        List<string> boughtUnlocks = game.Progress.Rules.Unlocks
            .Where(game.Progress.IsBought)
            .Select(unlock => unlock.Id)
            .ToList();
        var dto = new SaveDto(CurrentVersion, items, boughtUnlocks, game.Language, game.Scripts, ToFieldDto(game.Progress.Field));
        string text = JsonSerializer.Serialize(dto, _jsonOptions) + "\n";

        string temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, text);
        File.Move(temporaryPath, path, overwrite: true);
    }

    /// <summary>
    /// Reads a save file and checks it against the game rules. A missing file is a new player and gives a new game.
    /// A version 1 file has no field and gives a fresh start field.
    /// </summary>
    /// <param name="path">The path of the save file.</param>
    /// <param name="rules">The game rules that the item names, the unlock ids, the crops and the language must match.</param>
    /// <returns>The saved game, or a new game when there is no file.</returns>
    /// <exception cref="SaveException">The file is broken, is from another save version, or breaks a rule such as an unknown unlock id, item or crop, a tile that does not fit the field, or a drone outside the field.</exception>
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

        // Only the field size is needed to check the tiles. The progress built here is replaced by the one below.
        FieldSize size = new GameProgress(rules, dto.Items, boughtIds, null, default).FieldSize;
        Tile[,]? tiles = null;
        TilePosition dronePosition = default;
        if (dto.Field is { } field)
        {
            (tiles, dronePosition) = ReadField(fileName, field, rules, size);
        }
        else if (dto.Version == CurrentVersion)
        {
            throw new SaveException(fileName, "field is missing");
        }

        return new SavedGame(new GameProgress(rules, dto.Items, boughtIds, tiles, dronePosition), dto.Language, dto.Scripts);
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

        if (version is not (1 or CurrentVersion))
        {
            throw new SaveException(fileName, $"version {version} is not supported, this game reads versions 1 and {CurrentVersion}");
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

    private static (Tile[,] Tiles, TilePosition DronePosition) ReadField(string fileName, FieldDto field, GameRules rules, FieldSize size)
    {
        int tileCount = size.Width * size.Height;
        if (field.Tiles.Count != tileCount)
        {
            throw new SaveException(fileName, $"field has {field.Tiles.Count} tiles, expected {tileCount} for a {size.Width}x{size.Height} field");
        }

        var dronePosition = new TilePosition(field.Drone.X, field.Drone.Y);
        if (dronePosition.X < 0 || dronePosition.X >= size.Width || dronePosition.Y < 0 || dronePosition.Y >= size.Height)
        {
            throw new SaveException(fileName, $"drone ({dronePosition.X}, {dronePosition.Y}) is outside the {size.Width}x{size.Height} field");
        }

        var tiles = new Tile[size.Width, size.Height];
        for (int index = 0; index < tileCount; index++)
        {
            int x = index % size.Width;
            int y = index / size.Width;
            tiles[x, y] = ReadTile(fileName, field.Tiles[index], rules, x, y);
        }

        return (tiles, dronePosition);
    }

    private static Tile ReadTile(string fileName, TileDto tile, GameRules rules, int x, int y)
    {
        string where = $"tile ({x}, {y})";
        if (tile.Crop is null)
        {
            if (tile.AgeTicks != 0)
            {
                throw new SaveException(fileName, $"{where}: an empty tile has age 0, was {tile.AgeTicks}");
            }

            return new Tile(tile.Ground, null, 0);
        }

        Crop crop = rules.Crops.FirstOrDefault(candidate => candidate.Name == tile.Crop)
            ?? throw new SaveException(fileName, $"{where}: unknown crop {tile.Crop}");
        if (crop.Ground != tile.Ground)
        {
            throw new SaveException(fileName, $"{where}: {crop.Name} grows on {crop.Ground}, not {tile.Ground}");
        }

        if (tile.AgeTicks < 0 || tile.AgeTicks > crop.GrowTimeTicks)
        {
            throw new SaveException(fileName, $"{where}: age must be from 0 to {crop.GrowTimeTicks}, was {tile.AgeTicks}");
        }

        return new Tile(tile.Ground, crop, tile.AgeTicks);
    }
}
