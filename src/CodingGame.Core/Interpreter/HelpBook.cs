using System.Text.Json;
using System.Text.Json.Serialization;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;

namespace CodingGame.Core.Interpreter;

/// <summary>The kind of thing a help entry describes.</summary>
public enum HelpKind
{
    /// <summary>A drone command, such as move.</summary>
    Command,

    /// <summary>A constant: a direction, a kind of ground or a crop.</summary>
    Constant,

    /// <summary>A language keyword, such as for or ==.</summary>
    Keyword,
}

/// <summary>One line of help for the player.</summary>
/// <param name="Name">The name as in data/unlocks.json: a command, a constant or a keyword such as for or ==.</param>
/// <param name="Kind">What the name is.</param>
/// <param name="Summary">One sentence about what it does.</param>
/// <param name="Example">A short example in the language that was asked for. It may span several lines, separated by \n.</param>
public sealed record HelpEntry(string Name, HelpKind Kind, string Summary, string Example);

/// <summary>The help texts of the game, read from help.json and checked against the drone API.</summary>
public sealed class HelpBook
{
    private const string FileName = "help.json";

    // These have no table of their own; they are the same names as in data/unlocks.json.
    private static readonly string[] _keywords = ["for", "while", "if", "def", "=="];

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly List<HelpText> _texts;

    private HelpBook(List<HelpText> texts)
    {
        _texts = texts;
    }

    /// <summary>
    /// Reads and checks help.json from a data folder. Every command, constant and keyword must have exactly one entry.
    /// </summary>
    /// <param name="dataFolderPath">The folder that contains help.json.</param>
    /// <param name="api">The drone API whose commands and constants the entries must cover.</param>
    /// <returns>The validated help texts.</returns>
    /// <exception cref="GameRulesException">The file is missing or not valid JSON, an entry names something unknown or is repeated, an entry has an empty text, or a command, constant or keyword has no entry.</exception>
    public static HelpBook Load(string dataFolderPath, DroneApi api)
    {
        string path = Path.Combine(dataFolderPath, FileName);
        if (!File.Exists(path))
        {
            throw new GameRulesException(FileName, "file not found");
        }

        List<HelpDto>? dtos;
        try
        {
            dtos = JsonSerializer.Deserialize<List<HelpDto>>(File.ReadAllText(path), _jsonOptions);
        }
        catch (JsonException exception)
        {
            throw new GameRulesException(FileName, $"invalid JSON: {exception.Message}");
        }

        if (dtos is null)
        {
            throw new GameRulesException(FileName, "file is empty");
        }

        var kinds = new Dictionary<string, HelpKind>();
        foreach (string name in api.Commands.Keys)
        {
            kinds[name] = HelpKind.Command;
        }

        foreach (string name in api.Constants.Keys)
        {
            kinds[name] = HelpKind.Constant;
        }

        foreach (string name in _keywords)
        {
            kinds[name] = HelpKind.Keyword;
        }

        var texts = new List<HelpText>();
        var names = new HashSet<string>();
        foreach (HelpDto dto in dtos)
        {
            if (!kinds.TryGetValue(dto.Name, out HelpKind kind))
            {
                throw new GameRulesException(FileName, $"unknown name '{dto.Name}'");
            }

            if (!names.Add(dto.Name))
            {
                throw new GameRulesException(FileName, $"duplicate entry '{dto.Name}'");
            }

            if (string.IsNullOrWhiteSpace(dto.Summary) || string.IsNullOrWhiteSpace(dto.Python) || string.IsNullOrWhiteSpace(dto.JavaScript))
            {
                throw new GameRulesException(FileName, $"entry '{dto.Name}': summary, python and javascript must not be empty");
            }

            texts.Add(new HelpText(dto.Name, kind, dto.Summary, dto.Python, dto.JavaScript));
        }

        foreach (string name in kinds.Keys)
        {
            if (!names.Contains(name))
            {
                throw new GameRulesException(FileName, $"no entry for '{name}'");
            }
        }

        return new HelpBook(texts);
    }

    /// <summary>
    /// Gets the entries the player can use now, in the order of help.json. An entry is left out while one of its
    /// features is still locked.
    /// </summary>
    /// <param name="progress">The player's progress, which decides which features are locked.</param>
    /// <param name="language">The language whose example text is returned.</param>
    /// <returns>The entries for the unlocked names, with the example in the given language.</returns>
    public IReadOnlyList<HelpEntry> EntriesFor(GameProgress progress, ScriptLanguage language)
    {
        IReadOnlySet<string> locked = progress.LockedFeatures;
        return _texts
            .Where(text => !locked.Contains(text.Name))
            .Select(text => new HelpEntry(text.Name, text.Kind, text.Summary, ExampleIn(text, language)))
            .ToList();
    }

    private static string ExampleIn(HelpText text, ScriptLanguage language)
    {
        return language switch
        {
            ScriptLanguage.Python => text.Python,
            ScriptLanguage.JavaScript => text.JavaScript,
            _ => throw new InvalidOperationException("unknown script language"),
        };
    }

    private sealed record HelpDto(string Name, string Summary, string Python, [property: JsonPropertyName("javascript")] string JavaScript);

    private sealed record HelpText(string Name, HelpKind Kind, string Summary, string Python, string JavaScript);
}
