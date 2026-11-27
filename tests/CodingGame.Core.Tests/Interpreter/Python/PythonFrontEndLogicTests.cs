using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.Python;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter.Python;

public class PythonFrontEndLogicTests
{
    [Theory]
    [InlineData("if True and False:\n    harvest()", "1: if (true and false)")]
    [InlineData("if True or False:\n    harvest()", "1: if (true or false)")]
    [InlineData("if not True:\n    harvest()", "1: if (not true)")]
    [InlineData("if not not True:\n    harvest()", "1: if (not (not true))")]
    [InlineData("if True or False and False:\n    harvest()", "1: if (true or (false and false))")]
    [InlineData("if True and False or True:\n    harvest()", "1: if ((true and false) or true)")]
    [InlineData("if not 1 == 2 or 3 == 3:\n    harvest()", "1: if ((not (1 == 2)) or (3 == 3))")]
    [InlineData("if 1 == 2 and not 3 == 4:\n    harvest()", "1: if ((1 == 2) and (not (3 == 4)))")]
    [InlineData("if ripe() and ground() == SOIL:\n    harvest()", "1: if (ripe() and (ground() == SOIL))")]
    public void LogicOperatorsFollowPythonPrecedence(string source, string expectedDump)
    {
        ParseResult result = PythonFrontEnd.Parse(source);

        Assert.Null(result.Error);
        Assert.StartsWith(expectedDump, TreeDump.Of(result.Program!));
    }

    [Fact]
    public void ElifChainOfThreeIsNestedIfsInTheElseList()
    {
        const string source = """
        if x == 1:
            harvest()
        elif x == 2:
            harvest()
        elif x == 3:
            harvest()
        else:
            move(EAST)
        """;

        ParseResult result = PythonFrontEnd.Parse(source);

        Assert.Null(result.Error);
        Assert.Equal(
            "1: if (x == 1)\n  2: harvest()\nelse\n  3: if (x == 2)\n    4: harvest()\n  else\n    5: if (x == 3)\n      6: harvest()\n    else\n      8: move(EAST)",
            TreeDump.Of(result.Program!));
    }
}
