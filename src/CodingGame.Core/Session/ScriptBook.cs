using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;

namespace CodingGame.Core.Session;

/// <summary>
/// The player's scripts: an ordered list of named scripts with one of them selected. Each script has a Python and a
/// JavaScript text. The editor shows the selected script in the selected language, and Run runs the selected script.
/// </summary>
public sealed class ScriptBook
{
    /// <summary>
    /// The name of the script that a new player starts with.
    /// </summary>
    public const string FirstScriptName = "main";

    /// <summary>
    /// The most characters a script name may have, counted after the spaces at both ends are removed.
    /// </summary>
    public const int MaxNameLength = 20;

    private const string NewScriptNamePrefix = "script ";

    private readonly GameRules _rules;
    private readonly List<SavedScript> _scripts;
    private string _selected;

    /// <summary>
    /// Creates a book with one selected script, named <see cref="FirstScriptName"/>, that has the starter texts.
    /// </summary>
    /// <param name="rules">The game rules, with the starter texts.</param>
    public ScriptBook(GameRules rules)
        : this(
            rules,
            [new SavedScript(FirstScriptName, rules.StarterText(ScriptLanguage.Python), rules.StarterText(ScriptLanguage.JavaScript))],
            FirstScriptName)
    {
    }

    /// <summary>
    /// Creates a book from scripts that were read from a save file.
    /// </summary>
    /// <param name="rules">The game rules, with the starter texts that <see cref="Add"/> uses.</param>
    /// <param name="scripts">The scripts in tab order. Each name must follow the name rule and be unique.</param>
    /// <param name="selectedName">The name of the selected script. It must be the name of one of <paramref name="scripts"/>.</param>
    /// <exception cref="ArgumentException">The list is empty, a name breaks the name rule or is not unique, or <paramref name="selectedName"/> is not the name of a script.</exception>
    public ScriptBook(GameRules rules, IReadOnlyList<SavedScript> scripts, string selectedName)
    {
        if (scripts.Count == 0)
        {
            throw new ArgumentException("at least one script is needed");
        }

        var names = new List<string>();
        foreach (SavedScript script in scripts)
        {
            CheckName(script.Name, names);
            names.Add(script.Name);
        }

        if (!names.Contains(selectedName))
        {
            throw new ArgumentException($"no script named {selectedName}");
        }

        _rules = rules;
        _scripts = scripts.ToList();
        _selected = selectedName;
    }

    /// <summary>
    /// Gets the names of the scripts in tab order. Each call returns a new list.
    /// </summary>
    public IReadOnlyList<string> Names => _scripts.Select(script => script.Name).ToList();

    /// <summary>
    /// Gets the name of the selected script.
    /// </summary>
    public string Selected => _selected;

    /// <summary>
    /// Gets the text of a script in a language.
    /// </summary>
    /// <param name="name">The name of the script.</param>
    /// <param name="language">The language of the text.</param>
    /// <returns>The text of the script in that language.</returns>
    /// <exception cref="ArgumentException">There is no script with that name.</exception>
    public string Text(string name, ScriptLanguage language)
    {
        SavedScript script = _scripts[IndexOfScript(name)];
        return language switch
        {
            ScriptLanguage.Python => script.Python,
            ScriptLanguage.JavaScript => script.JavaScript,
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, "unknown script language"),
        };
    }

    /// <summary>
    /// Sets the text of a script in one language. The text of the other language is not changed.
    /// </summary>
    /// <param name="name">The name of the script.</param>
    /// <param name="language">The language of the text.</param>
    /// <param name="text">The new text.</param>
    /// <exception cref="ArgumentException">There is no script with that name.</exception>
    public void SetText(string name, ScriptLanguage language, string text)
    {
        int index = IndexOfScript(name);
        SavedScript script = _scripts[index];
        _scripts[index] = language switch
        {
            ScriptLanguage.Python => script with { Python = text },
            ScriptLanguage.JavaScript => script with { JavaScript = text },
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, "unknown script language"),
        };
    }

    /// <summary>
    /// Selects a script, so the editor shows it and Run runs it.
    /// </summary>
    /// <param name="name">The name of the script to select.</param>
    /// <exception cref="ArgumentException">There is no script with that name.</exception>
    public void Select(string name)
    {
        IndexOfScript(name);
        _selected = name;
    }

    /// <summary>
    /// Adds a script with the starter texts at the end and selects it. Its name is "script 2", "script 3" and so on,
    /// with the first number that no script has.
    /// </summary>
    /// <returns>The name of the new script.</returns>
    public string Add()
    {
        string name = FreeScriptName();
        _scripts.Add(new SavedScript(name, _rules.StarterText(ScriptLanguage.Python), _rules.StarterText(ScriptLanguage.JavaScript)));
        _selected = name;
        return name;
    }

    /// <summary>
    /// Gives a script a new name. The new name is trimmed of spaces at both ends. The text and the selection stay with the script.
    /// </summary>
    /// <param name="name">The current name of the script.</param>
    /// <param name="newName">The new name. After trimming it must be 1 to 20 characters, without a line break, and not the name of another script (case does not matter).</param>
    /// <exception cref="ArgumentException">There is no script with <paramref name="name"/>, or <paramref name="newName"/> breaks the name rule.</exception>
    public void Rename(string name, string newName)
    {
        int index = IndexOfScript(name);
        string trimmedName = newName.Trim();
        CheckName(trimmedName, _scripts.Where((_, position) => position != index).Select(script => script.Name));
        _scripts[index] = _scripts[index] with { Name = trimmedName };
        if (_selected == name)
        {
            _selected = trimmedName;
        }
    }

    /// <summary>
    /// Removes a script. When the removed script was selected, the script to its left is selected, or the new first
    /// script when the removed one was the first.
    /// </summary>
    /// <param name="name">The name of the script to remove.</param>
    /// <exception cref="ArgumentException">There is no script with that name.</exception>
    /// <exception cref="InvalidOperationException">It is the last script, and a book always keeps one.</exception>
    public void Remove(string name)
    {
        int index = IndexOfScript(name);
        if (_scripts.Count == 1)
        {
            throw new InvalidOperationException("the last script cannot be removed");
        }

        if (_selected == name)
        {
            _selected = _scripts[index > 0 ? index - 1 : 1].Name;
        }

        _scripts.RemoveAt(index);
    }

    private static void CheckName(string name, IEnumerable<string> otherNames)
    {
        if (name.Length is < 1 or > MaxNameLength)
        {
            throw new ArgumentException($"a name needs 1 to {MaxNameLength} characters");
        }

        if (name != name.Trim() || name.Contains('\n') || name.Contains('\r'))
        {
            throw new ArgumentException("a name cannot start or end with a space, or contain a line break");
        }

        if (otherNames.Any(other => string.Equals(other, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("a script with this name already exists");
        }
    }

    private int IndexOfScript(string name)
    {
        int index = _scripts.FindIndex(script => script.Name == name);
        return index >= 0 ? index : throw new ArgumentException($"no script named {name}");
    }

    private string FreeScriptName()
    {
        for (int number = 2; ; number++)
        {
            string name = NewScriptNamePrefix + number;
            if (!_scripts.Any(script => string.Equals(script.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return name;
            }
        }
    }
}
