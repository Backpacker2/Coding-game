using CodingGame.Core.Interpreter;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter;

public class TreeDumpTests
{
    [Fact]
    public void IfWithElseDumpsBothBlocks()
    {
        var conditional = new If(
            1,
            new Compare(1, new Call(1, "ground", []), CompareOperator.Equal, new Name(1, "SOIL")),
            [new ExpressionStatement(2, new Call(2, "plant", [new Name(2, "POTATO")]))],
            [new ExpressionStatement(4, new Call(4, "plant", [new Name(4, "RADISH")]))]);
        var program = new ScriptProgram([conditional], ScriptLanguage.Python);

        Assert.Equal(
            "1: if (ground() == SOIL)\n  2: plant(POTATO)\nelse\n  4: plant(RADISH)",
            TreeDump.Of(program));
    }

    [Fact]
    public void NestedLoopsAndCallsDumpWithDeeperIndentation()
    {
        var loop = new Repeat(
            1,
            new Literal(1, Value.FromNumber(2)),
            [
                new While(
                    2,
                    new Literal(2, Value.FromBoolean(true)),
                    [new ExpressionStatement(3, new Call(3, "f", [new Literal(3, Value.FromNumber(1)), new Name(3, "A")]))]),
            ],
            null);
        var program = new ScriptProgram([loop], ScriptLanguage.Python);

        Assert.Equal("1: repeat 2\n  2: while true\n    3: f(1, A)", TreeDump.Of(program));
    }

    [Fact]
    public void FunctionDefinitionThenStatementDumpsBothLines()
    {
        var definition = new FunctionDefinition(1, "f", []);
        var statement = new ExpressionStatement(
            3,
            new Compare(3, new Literal(3, Value.FromNumber(1)), CompareOperator.NotEqual, new Literal(3, Value.FromBoolean(false))));
        var program = new ScriptProgram([definition, statement], ScriptLanguage.Python);

        Assert.Equal("1: def f\n3: (1 != false)", TreeDump.Of(program));
    }

    [Fact]
    public void EmptyProgramDumpsToEmptyText()
    {
        var program = new ScriptProgram([], ScriptLanguage.Python);

        Assert.Equal(string.Empty, TreeDump.Of(program));
    }

    [Fact]
    public void LiteralThatIsNeitherNumberNorBooleanThrows()
    {
        var statement = new ExpressionStatement(1, new Literal(1, Value.None));
        var program = new ScriptProgram([statement], ScriptLanguage.Python);

        var exception = Assert.Throws<InvalidOperationException>(() => TreeDump.Of(program));

        Assert.Equal("unexpected literal kind None", exception.Message);
    }
}
