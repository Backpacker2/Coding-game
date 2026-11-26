using System.Text.Json;
using System.Text.Json.Serialization;
using CodingGame.Core.Interpreter;

namespace CodingGame.Core.Rules;

/// <summary>
/// The game rules read from the data folder: the crops, the tick costs, the starting field, the unlocks, the goals and the starter scripts.
/// </summary>
/// <param name="Crops">The crops, in the order they appear in crops.json.</param>
/// <param name="TickCosts">How many ticks each drone action and script step takes.</param>
/// <param name="StartField">The size of the field the game starts with.</param>
/// <param name="Unlocks">The unlocks, in the order they appear in unlocks.json.</param>
/// <param name="Goals">The first-steps goals, in the order they appear in goals.json.</param>
/// <param name="StarterTexts">The starter script of each language, from starter.py and starter.js.</param>
public sealed record GameRules(
    IReadOnlyList<Crop> Crops,
    TickCosts TickCosts,
    FieldSize StartField,
    IReadOnlyList<Unlock> Unlocks,
    IReadOnlyList<Goal> Goals,
    IReadOnlyDictionary<ScriptLanguage, string> StarterTexts)
{
    private const string CropsFileName = "crops.json";
    private const string TicksFileName = "ticks.json";
    private const string FieldFileName = "field.json";
    private const string UnlocksFileName = "unlocks.json";
    private const string GoalsFileName = "goals.json";
    private const string PythonStarterFileName = "starter.py";
    private const string JavaScriptStarterFileName = "starter.js";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false) },
    };

    // The goal kind is read as text and checked in ToGoal, so an unknown kind gets a message that names the goal.
    private sealed record GoalDto(string Id, string Text, string Hint, string Kind, string? Item = null, int? Count = null);

    /// <summary>
    /// Gets the starter script for a language: the text a new player sees in the editor.
    /// </summary>
    /// <param name="language">The script language.</param>
    /// <returns>The text of the starter file for the language.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The language has no starter text.</exception>
    public string StarterText(ScriptLanguage language)
    {
        if (!StarterTexts.TryGetValue(language, out string? text))
        {
            throw new ArgumentOutOfRangeException(nameof(language), language, "unknown script language");
        }

        return text;
    }

    /// <summary>
    /// Reads and checks crops.json, ticks.json, field.json, unlocks.json, goals.json, starter.py and starter.js from a data folder.
    /// </summary>
    /// <param name="dataFolderPath">The folder that contains the data files.</param>
    /// <returns>The validated game rules.</returns>
    /// <exception cref="GameRulesException">A file is missing, is not valid JSON for the rules, or breaks a rule such as a non-positive number, a duplicate crop, an unknown seed item, a missing wild crop, or a goal with an unknown kind or item.</exception>
    public static GameRules Load(string dataFolderPath)
    {
        List<Crop> crops = ReadFile<List<Crop>>(dataFolderPath, CropsFileName);
        TickCosts tickCosts = ReadFile<TickCosts>(dataFolderPath, TicksFileName);
        FieldSize startField = ReadFile<FieldSize>(dataFolderPath, FieldFileName);
        List<Unlock> unlocks = ReadFile<List<Unlock>>(dataFolderPath, UnlocksFileName);
        List<GoalDto> goalDtos = ReadFile<List<GoalDto>>(dataFolderPath, GoalsFileName);
        ValidateCrops(crops);
        ValidateTickCosts(tickCosts);
        ValidateStartField(startField);
        ValidateUnlocks(unlocks, crops, startField);
        List<Goal> goals = ReadGoals(goalDtos, crops);
        var starterTexts = new Dictionary<ScriptLanguage, string>
        {
            [ScriptLanguage.Python] = ReadText(dataFolderPath, PythonStarterFileName),
            [ScriptLanguage.JavaScript] = ReadText(dataFolderPath, JavaScriptStarterFileName),
        };
        return new GameRules(crops, tickCosts, startField, unlocks, goals, starterTexts);
    }

    private static string ReadText(string dataFolderPath, string fileName)
    {
        string path = Path.Combine(dataFolderPath, fileName);
        if (!File.Exists(path))
        {
            throw new GameRulesException(fileName, "file not found");
        }

        return File.ReadAllText(path);
    }

    private static T ReadFile<T>(string dataFolderPath, string fileName) where T : class
    {
        string json = ReadText(dataFolderPath, fileName);
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

    private static void ValidateUnlocks(List<Unlock> unlocks, List<Crop> crops, FieldSize startField)
    {
        var ids = new HashSet<string>();
        var features = new HashSet<string>();
        foreach (Unlock unlock in unlocks)
        {
            if (!ids.Add(unlock.Id))
            {
                throw new GameRulesException(UnlocksFileName, $"duplicate unlock id {unlock.Id}");
            }

            if (!crops.Any(crop => crop.Name == unlock.Price.Item))
            {
                throw new GameRulesException(UnlocksFileName, $"{unlock.Id}: price item {unlock.Price.Item} is not a crop");
            }

            RequirePositive(UnlocksFileName, unlock.Id, "price.count", unlock.Price.Count);
            foreach (string feature in unlock.Features)
            {
                if (!features.Add(feature))
                {
                    throw new GameRulesException(UnlocksFileName, $"feature {feature} is in more than one unlock");
                }
            }

            if (unlock.Features.Count == 0 && unlock.FieldSize is null)
            {
                throw new GameRulesException(UnlocksFileName, $"{unlock.Id}: unlock does nothing");
            }

            if (unlock.FieldSize is { } size && (size.Width <= startField.Width || size.Height <= startField.Height))
            {
                throw new GameRulesException(UnlocksFileName, $"{unlock.Id}: fieldSize must be larger than the start field");
            }
        }
    }

    private static List<Goal> ReadGoals(List<GoalDto> dtos, List<Crop> crops)
    {
        var ids = new HashSet<string>();
        var goals = new List<Goal>();
        foreach (GoalDto dto in dtos)
        {
            if (!ids.Add(dto.Id))
            {
                throw new GameRulesException(GoalsFileName, $"duplicate goal id {dto.Id}");
            }

            if (string.IsNullOrWhiteSpace(dto.Text))
            {
                throw new GameRulesException(GoalsFileName, $"{dto.Id}: text is empty");
            }

            if (string.IsNullOrWhiteSpace(dto.Hint))
            {
                throw new GameRulesException(GoalsFileName, $"{dto.Id}: hint is empty");
            }

            goals.Add(ToGoal(dto, crops));
        }

        return goals;
    }

    private static Goal ToGoal(GoalDto dto, List<Crop> crops)
    {
        GoalKind kind = dto.Kind switch
        {
            "finishRun" => GoalKind.FinishRun,
            "haveItems" => GoalKind.HaveItems,
            "visitAllTiles" => GoalKind.VisitAllTiles,
            "buyUnlock" => GoalKind.BuyUnlock,
            _ => throw new GameRulesException(GoalsFileName, $"{dto.Id}: unknown kind {dto.Kind}"),
        };
        if (kind != GoalKind.HaveItems)
        {
            if (dto.Item is not null || dto.Count is not null)
            {
                throw new GameRulesException(GoalsFileName, $"{dto.Id}: item and count are only for haveItems");
            }

            return new Goal(dto.Id, dto.Text, dto.Hint, kind, null, 0);
        }

        if (dto.Item is not { } item)
        {
            throw new GameRulesException(GoalsFileName, $"{dto.Id}: haveItems needs an item");
        }

        if (!crops.Any(crop => crop.Name == item))
        {
            throw new GameRulesException(GoalsFileName, $"{dto.Id}: item {item} is not a crop");
        }

        if (dto.Count is not { } count)
        {
            throw new GameRulesException(GoalsFileName, $"{dto.Id}: haveItems needs a count");
        }

        RequirePositive(GoalsFileName, dto.Id, "count", count);
        return new Goal(dto.Id, dto.Text, dto.Hint, kind, item, count);
    }

    private static void RequirePositive(string fileName, string owner, string jsonFieldName, int value)
    {
        if (value <= 0)
        {
            throw new GameRulesException(fileName, $"{owner}: {jsonFieldName} must be greater than 0, was {value}");
        }
    }
}
