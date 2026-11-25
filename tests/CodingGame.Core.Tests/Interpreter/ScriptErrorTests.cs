using CodingGame.Core.Interpreter;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter;

public class ScriptErrorTests
{
    [Fact]
    public void UndefinedNameInPythonSaysNameError()
    {
        var error = new ScriptError(ScriptErrorKind.UndefinedName, 3, "mvoe");

        Assert.Equal("NameError on line 3: name 'mvoe' is not defined", error.ToMessage(ScriptLanguage.Python));
    }

    [Fact]
    public void UndefinedNameInJavaScriptSaysReferenceError()
    {
        var error = new ScriptError(ScriptErrorKind.UndefinedName, 3, "mvoe");

        Assert.Equal("ReferenceError on line 3: mvoe is not defined", error.ToMessage(ScriptLanguage.JavaScript));
    }

    [Fact]
    public void WrongArgumentInPythonSaysTypeError()
    {
        var error = new ScriptError(ScriptErrorKind.WrongArgument, 2, "move expects direction, got number");

        Assert.Equal("TypeError on line 2: move expects direction, got number", error.ToMessage(ScriptLanguage.Python));
    }

    [Fact]
    public void WrongArgumentInJavaScriptSaysTypeError()
    {
        var error = new ScriptError(ScriptErrorKind.WrongArgument, 2, "move expects direction, got number");

        Assert.Equal("TypeError on line 2: move expects direction, got number", error.ToMessage(ScriptLanguage.JavaScript));
    }

    [Fact]
    public void NotUnlockedInPythonSaysGenericError()
    {
        var error = new ScriptError(ScriptErrorKind.NotUnlocked, 5, "import");

        Assert.Equal("Error on line 5: 'import' is not unlocked yet", error.ToMessage(ScriptLanguage.Python));
    }

    [Fact]
    public void NotUnlockedInJavaScriptSaysGenericError()
    {
        var error = new ScriptError(ScriptErrorKind.NotUnlocked, 5, "import");

        Assert.Equal("Error on line 5: 'import' is not unlocked yet", error.ToMessage(ScriptLanguage.JavaScript));
    }

    [Fact]
    public void CommandFailedInPythonSaysRuntimeError()
    {
        var error = new ScriptError(ScriptErrorKind.CommandFailed, 4, "cannot plant RADISH: not enough seed items");

        Assert.Equal("RuntimeError on line 4: cannot plant RADISH: not enough seed items", error.ToMessage(ScriptLanguage.Python));
    }

    [Fact]
    public void CommandFailedInJavaScriptSaysGenericError()
    {
        var error = new ScriptError(ScriptErrorKind.CommandFailed, 4, "cannot plant RADISH: not enough seed items");

        Assert.Equal("Error on line 4: cannot plant RADISH: not enough seed items", error.ToMessage(ScriptLanguage.JavaScript));
    }

    [Fact]
    public void TooDeepInPythonSaysRecursionError()
    {
        var error = new ScriptError(ScriptErrorKind.TooDeep, 7, "f");

        Assert.Equal("RecursionError on line 7: maximum recursion depth exceeded in f()", error.ToMessage(ScriptLanguage.Python));
    }

    [Fact]
    public void TooDeepInJavaScriptSaysRangeError()
    {
        var error = new ScriptError(ScriptErrorKind.TooDeep, 7, "f");

        Assert.Equal("RangeError on line 7: Maximum call stack size exceeded in f", error.ToMessage(ScriptLanguage.JavaScript));
    }

    [Fact]
    public void SyntaxErrorInPythonSaysSyntaxError()
    {
        var error = new ScriptError(ScriptErrorKind.SyntaxError, 3, "expected ':'");

        Assert.Equal("SyntaxError on line 3: expected ':'", error.ToMessage(ScriptLanguage.Python));
    }

    [Fact]
    public void SyntaxErrorInJavaScriptSaysSyntaxError()
    {
        var error = new ScriptError(ScriptErrorKind.SyntaxError, 3, "expected ':'");

        Assert.Equal("SyntaxError on line 3: expected ':'", error.ToMessage(ScriptLanguage.JavaScript));
    }

    [Fact]
    public void IndentationErrorInPythonSaysIndentationError()
    {
        var error = new ScriptError(ScriptErrorKind.IndentationError, 2, "unexpected indent");

        Assert.Equal("IndentationError on line 2: unexpected indent", error.ToMessage(ScriptLanguage.Python));
    }

    [Fact]
    public void ParseResultSuccessHasProgramAndNoError()
    {
        var program = new ScriptProgram([], ScriptLanguage.Python);

        ParseResult result = ParseResult.Success(program);

        Assert.Equal(program, result.Program);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ParseResultFailureHasErrorAndNoProgram()
    {
        var error = new ScriptError(ScriptErrorKind.SyntaxError, 1, "invalid syntax");

        ParseResult result = ParseResult.Failure(error);

        Assert.Null(result.Program);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void StepResultRunningHasNoError()
    {
        Assert.Equal(StepStatus.Running, StepResult.Running.Status);
        Assert.Null(StepResult.Running.Error);
    }

    [Fact]
    public void StepResultFinishedHasNoError()
    {
        Assert.Equal(StepStatus.Finished, StepResult.Finished.Status);
        Assert.Null(StepResult.Finished.Error);
    }

    [Fact]
    public void StepResultFailedCarriesTheError()
    {
        var error = new ScriptError(ScriptErrorKind.UndefinedName, 1, "mvoe");

        StepResult result = StepResult.Failed(error);

        Assert.Equal(StepStatus.Failed, result.Status);
        Assert.Equal(error, result.Error);
    }
}
