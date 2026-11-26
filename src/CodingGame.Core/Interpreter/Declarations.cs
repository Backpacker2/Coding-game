namespace CodingGame.Core.Interpreter;

/// <summary>
/// The names a script declares anywhere in its text: loop counters, variables and player functions.
/// </summary>
/// <param name="Counters">The loop counter names, from every for loop in the script.</param>
/// <param name="Variables">The names that are assigned anywhere in the script.</param>
/// <param name="Functions">The names of the player's functions.</param>
internal sealed record DeclaredNames(
    IReadOnlySet<string> Counters,
    IReadOnlySet<string> Variables,
    IReadOnlySet<string> Functions);

/// <summary>
/// Collects the names a script declares, so that a name read can be told apart from a command or constant.
/// </summary>
internal static class Declarations
{
    /// <summary>
    /// Collects the declared names of a parsed script.
    /// </summary>
    /// <param name="program">The parsed script.</param>
    /// <returns>The declared names, from the whole script.</returns>
    internal static DeclaredNames Of(ScriptProgram program)
    {
        var counters = new HashSet<string>();
        var variables = new HashSet<string>();
        var functions = new HashSet<string>();
        Collect(program.Body, counters, variables, functions);
        return new DeclaredNames(counters, variables, functions);
    }

    private static void Collect(
        IReadOnlyList<Statement> statements,
        HashSet<string> counters,
        HashSet<string> variables,
        HashSet<string> functions)
    {
        foreach (Statement statement in statements)
        {
            switch (statement)
            {
                case Assign assignment:
                    variables.Add(assignment.Name);
                    break;
                case If conditional:
                    Collect(conditional.Then, counters, variables, functions);
                    Collect(conditional.Else, counters, variables, functions);
                    break;
                case While loop:
                    Collect(loop.Body, counters, variables, functions);
                    break;
                case Repeat repeat:
                    if (repeat.Counter is { } counter)
                    {
                        counters.Add(counter);
                    }

                    Collect(repeat.Body, counters, variables, functions);
                    break;
                case FunctionDefinition definition:
                    functions.Add(definition.Name);
                    Collect(definition.Body, counters, variables, functions);
                    break;
            }
        }
    }
}
