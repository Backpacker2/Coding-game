using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Session;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter;

public class LessonExampleTests
{
    private const int MaxTicks = 10_000;

    private static readonly GameRules _rules = EvaluatorTestSupport.Rules();

    /// <summary>
    /// Lists one row per lesson and language, so every lesson example is checked in both languages.
    /// </summary>
    /// <returns>The lesson title and the language to run its example in.</returns>
    public static TheoryData<string, ScriptLanguage> LessonRows()
    {
        var rows = new TheoryData<string, ScriptLanguage>();
        foreach (ScriptLanguage language in Enum.GetValues<ScriptLanguage>())
        {
            foreach (HelpEntry entry in LessonsFor(language))
            {
                rows.Add(entry.Name, language);
            }
        }

        return rows;
    }

    /// <summary>
    /// Runs the example of one lesson with every unlock bought. The example must finish without an error,
    /// so a lesson never teaches a script that the game refuses.
    /// </summary>
    /// <param name="title">The title of the lesson.</param>
    /// <param name="language">The language of the example.</param>
    [Theory]
    [MemberData(nameof(LessonRows))]
    public void LessonExampleFinishesWithEverythingUnlocked(string title, ScriptLanguage language)
    {
        string example = LessonsFor(language).Single(entry => entry.Name == title).Example;
        RunSession session = RunSession.Start(example, language, EvaluatorTestSupport.AllUnlocked(_rules));

        for (int tick = 0; tick < MaxTicks && session.Status == StepStatus.Running; tick++)
        {
            session.StepTick();
        }

        Assert.Equal(StepStatus.Finished, session.Status);
        Assert.Null(session.ErrorMessage);
    }

    private static List<HelpEntry> LessonsFor(ScriptLanguage language)
    {
        var book = HelpBook.Load(
            Path.Combine(AppContext.BaseDirectory, "data"),
            new DroneApi(_rules, EvaluatorTestSupport.NewField(_rules)));
        return book.EntriesFor(EvaluatorTestSupport.AllUnlocked(_rules), language)
            .Where(entry => entry.Kind == HelpKind.Lesson)
            .ToList();
    }
}
