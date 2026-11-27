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

    /// <summary>A short lesson about the language.</summary>
    Lesson,

    /// <summary>The first lesson the player has not unlocked yet.</summary>
    NextLesson,
}

/// <summary>One line of help for the player.</summary>
/// <param name="Name">The name as in data/unlocks.json: a command, a constant or a keyword such as for or ==. For a lesson, its title.</param>
/// <param name="Kind">What the name is.</param>
/// <param name="Summary">One sentence about what it does.</param>
/// <param name="Example">A short example in the language that was asked for. It may span several lines, separated by \n.</param>
public sealed record HelpEntry(string Name, HelpKind Kind, string Summary, string Example);

/// <summary>The help texts of the game, read from help.json and lessons.json, and checked against the drone API.</summary>
public sealed class HelpBook
{
    private const string FileName = "help.json";
    private const string LessonsFileName = "lessons.json";

    // These have no table of their own; they are the same names as in data/unlocks.json. "+" stands for + - *,
    // "<" for < > <= >= and "and" for and, or and not, one help entry each, gated by the first feature of the group.
    private static readonly string[] _keywords = ["for", "while", "if", "def", "==", "variables", "+", "<", "and"];

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly List<HelpText> _texts;
    private readonly List<LessonDto> _lessons;

    private HelpBook(List<HelpText> texts, List<LessonDto> lessons)
    {
        _texts = texts;
        _lessons = lessons;
    }

    /// <summary>
    /// Reads and checks help.json and lessons.json from a data folder. Every command, constant and keyword must have exactly one entry.
    /// </summary>
    /// <param name="dataFolderPath">The folder that contains help.json and lessons.json.</param>
    /// <param name="api">The drone API whose commands and constants the entries must cover.</param>
    /// <returns>The validated help texts and lessons.</returns>
    /// <exception cref="GameRulesException">A file is missing, empty or not valid JSON, an entry names something unknown or is repeated, an entry or lesson has an empty text, a lesson names an unknown feature, or a command, constant or keyword has no entry.</exception>
    public static HelpBook Load(string dataFolderPath, DroneApi api)
    {
        List<HelpDto> dtos = ReadJson<HelpDto>(dataFolderPath, FileName);
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

        List<LessonDto> lessons = ReadJson<LessonDto>(dataFolderPath, LessonsFileName);
        CheckLessons(lessons);
        return new HelpBook(texts, lessons);
    }

    /// <summary>
    /// Gets the entries the player can use now. First come the lessons that are shown, in the order of lessons.json.
    /// A lesson is shown when it has no feature or its feature is unlocked. Then comes the first lesson that is still
    /// locked, with the unlock that teaches it. Last come the commands, constants and keywords, in the order of help.json.
    /// An entry for a command, constant or keyword is left out while one of its features is still locked.
    /// </summary>
    /// <param name="progress">The player's progress, which decides which features are locked.</param>
    /// <param name="language">The language whose example and lesson text is returned.</param>
    /// <returns>The entries for the player, with the example and text in the given language.</returns>
    public IReadOnlyList<HelpEntry> EntriesFor(GameProgress progress, ScriptLanguage language)
    {
        IReadOnlyDictionary<string, Unlock> locked = progress.LockedFeatures;
        var entries = new List<HelpEntry>();
        (LessonDto Lesson, Unlock Unlock)? upcoming = null;
        foreach (LessonDto lesson in _lessons)
        {
            if (lesson.Feature is { } feature && locked.TryGetValue(feature, out Unlock? unlock))
            {
                upcoming ??= (lesson, unlock);
                continue;
            }

            LessonTextDto text = TextIn(lesson, language);
            entries.Add(new HelpEntry(lesson.Title, HelpKind.Lesson, text.Text, text.Example));
        }

        if (upcoming is { } next)
        {
            LessonTextDto text = TextIn(next.Lesson, language);
            entries.Add(new HelpEntry(
                next.Lesson.Title,
                HelpKind.NextLesson,
                $"Next: unlock \"{next.Unlock.Title}\" to learn this. {text.Text}",
                text.Example));
        }

        entries.AddRange(_texts
            .Where(text => !locked.ContainsKey(text.Name))
            .Select(text => new HelpEntry(text.Name, text.Kind, text.Summary, ExampleIn(text, language))));
        return entries;
    }

    private static List<T> ReadJson<T>(string dataFolderPath, string fileName)
    {
        string path = Path.Combine(dataFolderPath, fileName);
        if (!File.Exists(path))
        {
            throw new GameRulesException(fileName, "file not found");
        }

        List<T>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), _jsonOptions);
        }
        catch (JsonException exception)
        {
            throw new GameRulesException(fileName, $"invalid JSON: {exception.Message}");
        }

        return items ?? throw new GameRulesException(fileName, "file is empty");
    }

    private static void CheckLessons(List<LessonDto> lessons)
    {
        var ids = new HashSet<string>();
        foreach (LessonDto lesson in lessons)
        {
            if (!ids.Add(lesson.Id))
            {
                throw new GameRulesException(LessonsFileName, $"duplicate id '{lesson.Id}'");
            }

            if (lesson.Feature is { } feature && !_keywords.Contains(feature))
            {
                throw new GameRulesException(LessonsFileName, $"lesson '{lesson.Id}': unknown feature '{feature}'");
            }

            if (string.IsNullOrWhiteSpace(lesson.Title) || string.IsNullOrWhiteSpace(lesson.Python.Text) || string.IsNullOrWhiteSpace(lesson.Python.Example)
                || string.IsNullOrWhiteSpace(lesson.JavaScript.Text) || string.IsNullOrWhiteSpace(lesson.JavaScript.Example))
            {
                throw new GameRulesException(LessonsFileName, $"lesson '{lesson.Id}': title, python and javascript text and example must not be empty");
            }
        }
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

    private static LessonTextDto TextIn(LessonDto lesson, ScriptLanguage language)
    {
        return language switch
        {
            ScriptLanguage.Python => lesson.Python,
            ScriptLanguage.JavaScript => lesson.JavaScript,
            _ => throw new InvalidOperationException("unknown script language"),
        };
    }

    private sealed record HelpDto(string Name, string Summary, string Python, [property: JsonPropertyName("javascript")] string JavaScript);

    private sealed record HelpText(string Name, HelpKind Kind, string Summary, string Python, string JavaScript);

    private sealed record LessonDto(string Id, string Title, string? Feature, LessonTextDto Python, [property: JsonPropertyName("javascript")] LessonTextDto JavaScript);

    private sealed record LessonTextDto(string Text, string Example);
}
