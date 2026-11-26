using CodingGame.Core.Session;
using Xunit;

namespace CodingGame.Core.Tests.Session;

public class SaveExceptionTests
{
    [Fact]
    public void MessageNamesTheFileAndTheProblem()
    {
        var exception = new SaveException("save.json", "bad");

        Assert.Equal("save.json: bad", exception.Message);
    }
}
