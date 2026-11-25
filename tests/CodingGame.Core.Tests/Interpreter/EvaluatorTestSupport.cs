using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;

namespace CodingGame.Core.Tests.Interpreter;

internal static class EvaluatorTestSupport
{
    private const int MaxSteps = 10_000;

    internal static GameRules Rules()
    {
        return GameRules.Load(Path.Combine(AppContext.BaseDirectory, "data"));
    }

    internal static Field NewField(GameRules rules)
    {
        Crop clover = rules.Crops.Single(crop => crop.Name == "CLOVER");
        return new Field(3, 1, clover);
    }

    internal static ScriptProgram PythonProgram(params Statement[] body)
    {
        return new ScriptProgram(body, ScriptLanguage.Python);
    }

    internal static Statement CallStatement(int line, string callee, params Expression[] arguments)
    {
        return new ExpressionStatement(line, new Call(line, callee, arguments));
    }

    internal static Expression Constant(int line, string name)
    {
        return new Name(line, name);
    }

    internal static StepResult RunToEnd(Evaluator evaluator)
    {
        for (int step = 0; step < MaxSteps; step++)
        {
            StepResult result = evaluator.Step();
            if (result.Status != StepStatus.Running)
            {
                return result;
            }
        }

        throw new InvalidOperationException($"script still running after {MaxSteps} steps");
    }
}
