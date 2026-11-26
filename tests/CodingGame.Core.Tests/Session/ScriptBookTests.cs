using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using CodingGame.Core.Tests.Interpreter;
using Xunit;

namespace CodingGame.Core.Tests.Session;

public class ScriptBookTests
{
    private const string TwentyOneCharacters = "abcdefghijklmnopqrstu";
    private const string TwentyCharacters = "abcdefghijklmnopqrst";

    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    public static TheoryData<IReadOnlyList<SavedScript>, string, string> BadRestoreCases => new()
    {
        { [], "main", "at least one script is needed" },
        { [new SavedScript("main", "a", "b"), new SavedScript("MAIN", "c", "d")], "main", "a script with this name already exists" },
        { [new SavedScript("", "a", "b")], "", "a name needs 1 to 20 characters" },
        { [new SavedScript(" main", "a", "b")], " main", "a name cannot start or end with a space, or contain a line break" },
        { [new SavedScript("main", "a", "b")], "nope", "no script named nope" },
    };

    public static TheoryData<string, string> BadRenameCases => new()
    {
        { "   ", "a name needs 1 to 20 characters" },
        { TwentyOneCharacters, "a name needs 1 to 20 characters" },
        { "first\nsecond", "a name cannot start or end with a space, or contain a line break" },
        { "main", "a script with this name already exists" },
        { "MAIN", "a script with this name already exists" },
    };

    [Fact]
    public void NewBookHasOneSelectedScriptNamedMainWithStarterTexts()
    {
        var book = new ScriptBook(_rules);

        Assert.Equal(["main"], book.Names);
        Assert.Equal("main", book.Selected);
        Assert.Equal(_rules.StarterText(ScriptLanguage.Python), book.Text("main", ScriptLanguage.Python));
        Assert.Equal(_rules.StarterText(ScriptLanguage.JavaScript), book.Text("main", ScriptLanguage.JavaScript));
    }

    [Fact]
    public void SetTextChangesOnlyThatLanguageOfThatScript()
    {
        var book = new ScriptBook(_rules);
        book.Add();

        book.SetText("main", ScriptLanguage.Python, "harvest()\n");

        Assert.Equal("harvest()\n", book.Text("main", ScriptLanguage.Python));
        Assert.Equal(_rules.StarterText(ScriptLanguage.JavaScript), book.Text("main", ScriptLanguage.JavaScript));
        Assert.Equal(_rules.StarterText(ScriptLanguage.Python), book.Text("script 2", ScriptLanguage.Python));
    }

    [Fact]
    public void AddAppendsAndSelectsANewScriptWithStarterTexts()
    {
        var book = new ScriptBook(_rules);

        string name = book.Add();

        Assert.Equal("script 2", name);
        Assert.Equal(["main", "script 2"], book.Names);
        Assert.Equal("script 2", book.Selected);
        Assert.Equal(_rules.StarterText(ScriptLanguage.Python), book.Text("script 2", ScriptLanguage.Python));
        Assert.Equal(_rules.StarterText(ScriptLanguage.JavaScript), book.Text("script 2", ScriptLanguage.JavaScript));
    }

    [Fact]
    public void AddAfterRemoveReusesTheFirstFreeNumber()
    {
        var book = new ScriptBook(_rules);
        book.Add();
        book.Add();
        book.Remove("script 2");

        string name = book.Add();

        Assert.Equal("script 2", name);
        Assert.Equal(["main", "script 3", "script 2"], book.Names);
    }

    [Fact]
    public void SelectChangesTheSelectedScript()
    {
        var book = new ScriptBook(_rules);
        book.Add();

        book.Select("main");

        Assert.Equal("main", book.Selected);
    }

    [Fact]
    public void RenameKeepsTheTextAndTheSelection()
    {
        var book = new ScriptBook(_rules);
        book.Add();
        book.SetText("script 2", ScriptLanguage.JavaScript, "harvest();\n");

        book.Rename("script 2", "planting");

        Assert.Equal(["main", "planting"], book.Names);
        Assert.Equal("planting", book.Selected);
        Assert.Equal("harvest();\n", book.Text("planting", ScriptLanguage.JavaScript));
    }

    [Fact]
    public void RenameTrimsTheNewName()
    {
        var book = new ScriptBook(_rules);

        book.Rename("main", "  first  ");

        Assert.Equal(["first"], book.Names);
        Assert.Equal("first", book.Selected);
    }

    [Fact]
    public void RenameToTheSameNameWithOtherCaseIsAllowed()
    {
        var book = new ScriptBook(_rules);

        book.Rename("main", "Main");

        Assert.Equal(["Main"], book.Names);
    }

    [Fact]
    public void NameOfTwentyCharactersIsAccepted()
    {
        var book = new ScriptBook(_rules);

        book.Rename("main", TwentyCharacters);

        Assert.Equal([TwentyCharacters], book.Names);
    }

    [Theory]
    [MemberData(nameof(BadRenameCases))]
    public void RenameRejectsABadNameAndKeepsTheNames(string newName, string expectedMessage)
    {
        var book = new ScriptBook(_rules);
        book.Add();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => book.Rename("script 2", newName));

        Assert.Equal(expectedMessage, exception.Message);
        Assert.Equal(["main", "script 2"], book.Names);
    }

    [Fact]
    public void RemoveSelectedScriptSelectsTheLeftNeighbour()
    {
        var book = new ScriptBook(_rules);
        book.Add();
        book.Add();

        book.Remove("script 3");

        Assert.Equal(["main", "script 2"], book.Names);
        Assert.Equal("script 2", book.Selected);
    }

    [Fact]
    public void RemoveSelectedFirstScriptSelectsTheNewFirst()
    {
        var book = new ScriptBook(_rules);
        book.Add();
        book.Select("main");

        book.Remove("main");

        Assert.Equal(["script 2"], book.Names);
        Assert.Equal("script 2", book.Selected);
    }

    [Fact]
    public void RemoveUnselectedScriptKeepsTheSelection()
    {
        var book = new ScriptBook(_rules);
        book.Add();
        book.Add();
        book.Select("main");

        book.Remove("script 2");

        Assert.Equal(["main", "script 3"], book.Names);
        Assert.Equal("main", book.Selected);
    }

    [Fact]
    public void RemovingTheLastScriptIsRejected()
    {
        var book = new ScriptBook(_rules);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => book.Remove("main"));

        Assert.Equal("the last script cannot be removed", exception.Message);
        Assert.Equal(["main"], book.Names);
    }

    [Fact]
    public void UnknownNameIsRejectedByEveryMethod()
    {
        var book = new ScriptBook(_rules);
        book.Add();

        Assert.Throws<ArgumentException>(() => book.Text("nope", ScriptLanguage.Python));
        Assert.Throws<ArgumentException>(() => book.SetText("nope", ScriptLanguage.Python, "move(EAST)\n"));
        Assert.Throws<ArgumentException>(() => book.Select("nope"));
        Assert.Throws<ArgumentException>(() => book.Rename("nope", "first"));
        Assert.Throws<ArgumentException>(() => book.Remove("nope"));
    }

    [Fact]
    public void RestoredBookKeepsOrderTextsAndSelection()
    {
        SavedScript[] scripts = [new SavedScript("main", "a", "b"), new SavedScript("loop", "c", "d")];

        var book = new ScriptBook(_rules, scripts, "loop");

        Assert.Equal(["main", "loop"], book.Names);
        Assert.Equal("loop", book.Selected);
        Assert.Equal("c", book.Text("loop", ScriptLanguage.Python));
        Assert.Equal("d", book.Text("loop", ScriptLanguage.JavaScript));
    }

    [Theory]
    [MemberData(nameof(BadRestoreCases))]
    public void RestoreRejectsABrokenListWithTheExactMessage(IReadOnlyList<SavedScript> scripts, string selectedName, string expectedMessage)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ScriptBook(_rules, scripts, selectedName));

        Assert.Equal(expectedMessage, exception.Message);
    }
}
