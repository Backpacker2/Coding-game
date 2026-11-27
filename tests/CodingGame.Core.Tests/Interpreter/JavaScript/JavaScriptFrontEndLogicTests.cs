using CodingGame.Core.Interpreter;
using CodingGame.Core.Interpreter.JavaScript;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter.JavaScript;

public class JavaScriptFrontEndLogicTests
{
    [Theory]
    [InlineData("if (true && false) {\n    harvest();\n}", "1: if (true and false)")]
    [InlineData("if (true || false) {\n    harvest();\n}", "1: if (true or false)")]
    [InlineData("if (!true) {\n    harvest();\n}", "1: if (not true)")]
    [InlineData("if (!!true) {\n    harvest();\n}", "1: if (not (not true))")]
    [InlineData("if (true || false && false) {\n    harvest();\n}", "1: if (true or (false and false))")]
    [InlineData("if (true && false || true) {\n    harvest();\n}", "1: if ((true and false) or true)")]
    [InlineData("if (!1 === 2 || 3 === 3) {\n    harvest();\n}", "1: if (((not 1) == 2) or (3 == 3))")]
    [InlineData("if (1 === 2 && !(3 === 4)) {\n    harvest();\n}", "1: if ((1 == 2) and (not (3 == 4)))")]
    [InlineData("if (ripe() && ground() === SOIL) {\n    harvest();\n}", "1: if (ripe() and (ground() == SOIL))")]
    public void LogicOperatorsFollowJavaScriptPrecedence(string source, string expectedDump)
    {
        ParseResult result = JavaScriptFrontEnd.Parse(source);

        Assert.Null(result.Error);
        Assert.StartsWith(expectedDump, TreeDump.Of(result.Program!));
    }
}
