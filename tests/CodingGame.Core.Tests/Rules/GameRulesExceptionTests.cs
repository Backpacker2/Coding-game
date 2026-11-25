using CodingGame.Core.Rules;
using Xunit;

namespace CodingGame.Core.Tests.Rules;

public class GameRulesExceptionTests
{
    [Fact]
    public void MessageStartsWithFileNameFollowedByProblem()
    {
        var exception = new GameRulesException("crops.json", "bad");

        Assert.Equal("crops.json: bad", exception.Message);
    }
}
