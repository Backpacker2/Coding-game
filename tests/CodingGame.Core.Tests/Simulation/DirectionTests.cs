using CodingGame.Core.Simulation;
using Xunit;

namespace CodingGame.Core.Tests.Simulation;

public class DirectionTests
{
    [Fact]
    public void HasFourCompassDirectionsInClockwiseOrder()
    {
        Direction[] expected = [Direction.North, Direction.East, Direction.South, Direction.West];

        Assert.Equal(expected, Enum.GetValues<Direction>());
    }
}
