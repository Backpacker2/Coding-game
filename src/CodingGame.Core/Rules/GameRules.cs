using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodingGame.Core.Rules;

/// <summary>
/// The game rules read from the data folder: the crops, the tick costs and the starting field.
/// </summary>
/// <param name="Crops">The crops, in the order they appear in crops.json.</param>
/// <param name="TickCosts">How many ticks each drone action and script step takes.</param>
/// <param name="StartField">The size of the field the game starts with.</param>
public sealed record GameRules(IReadOnlyList<Crop> Crops, TickCosts TickCosts, FieldSize StartField)
{
    private const string CropsFileName = "crops.json";
    private const string TicksFileName = "ticks.json";
    private const string FieldFileName = "field.json";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false) },
    };

    /// <summary>
    /// Reads and checks crops.json, ticks.json and field.json from a data folder.
    /// </summary>
    /// <param name="dataFolderPath">The folder that contains crops.json, ticks.json and field.json.</param>
    /// <returns>The validated game rules.</returns>
    /// <exception cref="GameRulesException">A file is missing, is not valid JSON for the rules, or breaks a rule such as a non-positive number, a duplicate crop, an unknown seed item or a missing wild crop.</exception>
    public static GameRules Load(string dataFolderPath)
    {
        List<Crop> crops = ReadFile<List<Crop>>(dataFolderPath, CropsFileName);
        TickCosts tickCosts = ReadFile<TickCosts>(dataFolderPath, TicksFileName);
        FieldSize startField = ReadFile<FieldSize>(dataFolderPath, FieldFileName);
        ValidateCrops(crops);
        ValidateTickCosts(tickCosts);
        ValidateStartField(startField);
        return new GameRules(crops, tickCosts, startField);
    }

    private static T ReadFile<T>(string dataFolderPath, string fileName) where T : class
    {
        string path = Path.Combine(dataFolderPath, fileName);
        if (!File.Exists(path))
        {
            throw new GameRulesException(fileName, "file not found");
        }

        string json = File.ReadAllText(path);
        T? value;
        try
        {
            value = JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (JsonException exception)
        {
            throw new GameRulesException(fileName, exception.Message);
        }

        return value ?? throw new GameRulesException(fileName, "file is empty");
    }

    private static void ValidateCrops(List<Crop> crops)
    {
        if (crops.Count == 0)
        {
            throw new GameRulesException(CropsFileName, "no crops");
        }

        var names = new HashSet<string>();
        foreach (Crop crop in crops)
        {
            if (!names.Add(crop.Name))
            {
                throw new GameRulesException(CropsFileName, $"duplicate crop name {crop.Name}");
            }
        }

        foreach (Crop crop in crops)
        {
            RequirePositive(CropsFileName, crop.Name, "growTimeTicks", crop.GrowTimeTicks);
            RequirePositive(CropsFileName, crop.Name, "yieldItems", crop.YieldItems);
            if (crop.Seed is null)
            {
                continue;
            }

            RequirePositive(CropsFileName, crop.Name, "seed.count", crop.Seed.Count);
            if (!names.Contains(crop.Seed.Item))
            {
                throw new GameRulesException(CropsFileName, $"{crop.Name}: seed item {crop.Seed.Item} is not a crop");
            }
        }

        if (!crops.Any(crop => crop.Seed is null))
        {
            throw new GameRulesException(CropsFileName, "no wild crop (a crop with seed null)");
        }
    }

    private static void ValidateTickCosts(TickCosts tickCosts)
    {
        RequirePositive(TicksFileName, "ticks", "moveTicks", tickCosts.MoveTicks);
        RequirePositive(TicksFileName, "ticks", "harvestTicks", tickCosts.HarvestTicks);
        RequirePositive(TicksFileName, "ticks", "plantTicks", tickCosts.PlantTicks);
        RequirePositive(TicksFileName, "ticks", "ripeTicks", tickCosts.RipeTicks);
        RequirePositive(TicksFileName, "ticks", "tillTicks", tickCosts.TillTicks);
        RequirePositive(TicksFileName, "ticks", "groundTicks", tickCosts.GroundTicks);
        RequirePositive(TicksFileName, "ticks", "loopIterationTicks", tickCosts.LoopIterationTicks);
        RequirePositive(TicksFileName, "ticks", "functionCallTicks", tickCosts.FunctionCallTicks);
    }

    private static void ValidateStartField(FieldSize startField)
    {
        RequirePositive(FieldFileName, "field", "width", startField.Width);
        RequirePositive(FieldFileName, "field", "height", startField.Height);
    }

    private static void RequirePositive(string fileName, string owner, string jsonFieldName, int value)
    {
        if (value <= 0)
        {
            throw new GameRulesException(fileName, $"{owner}: {jsonFieldName} must be greater than 0, was {value}");
        }
    }
}
