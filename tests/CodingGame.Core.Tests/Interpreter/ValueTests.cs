using CodingGame.Core.Interpreter;
using CodingGame.Core.Rules;
using CodingGame.Core.Simulation;
using Xunit;

namespace CodingGame.Core.Tests.Interpreter;

public class ValueTests
{
    private static readonly Crop _clover = new("CLOVER", Ground.Grass, 200, 1, null, true);

    [Fact]
    public void NumberRoundTrips()
    {
        Assert.Equal(5, Value.FromNumber(5).AsNumber());
    }

    [Fact]
    public void BooleanRoundTrips()
    {
        Assert.True(Value.FromBoolean(true).AsBoolean());
        Assert.False(Value.FromBoolean(false).AsBoolean());
    }

    [Fact]
    public void DirectionRoundTrips()
    {
        Assert.Equal(Direction.West, Value.FromDirection(Direction.West).AsDirection());
    }

    [Fact]
    public void GroundRoundTrips()
    {
        Assert.Equal(Ground.Soil, Value.FromGround(Ground.Soil).AsGround());
    }

    [Fact]
    public void CropRoundTrips()
    {
        Assert.Equal(_clover, Value.FromCrop(_clover).AsCrop());
    }

    [Fact]
    public void FactoriesSetTheirKind()
    {
        Assert.Equal(ValueKind.Number, Value.FromNumber(1).Kind);
        Assert.Equal(ValueKind.Boolean, Value.FromBoolean(true).Kind);
        Assert.Equal(ValueKind.Direction, Value.FromDirection(Direction.North).Kind);
        Assert.Equal(ValueKind.Ground, Value.FromGround(Ground.Grass).Kind);
        Assert.Equal(ValueKind.Crop, Value.FromCrop(_clover).Kind);
    }

    [Fact]
    public void NoneHasKindNone()
    {
        Assert.Equal(ValueKind.None, Value.None.Kind);
    }

    [Fact]
    public void DefaultValueIsNone()
    {
        Assert.Equal(Value.None, default(Value));
    }

    [Fact]
    public void EqualValuesOfSameKindAreEqual()
    {
        Assert.Equal(Value.FromNumber(1), Value.FromNumber(1));
    }

    [Fact]
    public void ValuesOfDifferentKindsAreNotEqual()
    {
        Assert.NotEqual(Value.FromNumber(1), Value.FromBoolean(true));
    }

    [Fact]
    public void DifferentGroundsAreNotEqual()
    {
        Assert.NotEqual(Value.FromGround(Ground.Grass), Value.FromGround(Ground.Soil));
    }

    [Fact]
    public void ReadingAsTheWrongKindThrows()
    {
        Assert.Throws<InvalidOperationException>(() => Value.FromBoolean(true).AsNumber());
    }
}
