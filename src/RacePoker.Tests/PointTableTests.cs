using RacePoker.Core.Enums;
using RacePoker.Core.Models;
using RacePoker.Core.Services;
using System.Text;

namespace RacePoker.Tests;

public class PointTableTests
{
    [Fact]
    public void LoadFromStream_AppliesValidOverridesAndIgnoresInvalidValues()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(
            """{"TwoPair": 7100, "OnePair": -1, "RoyalFlush": 1000001, "Unknown": 5, "999": 42}"""));
        var table = PointTableService.Load(stream);
        Assert.Equal(7100, table.Points[HandCategory.TwoPair]);
        Assert.Equal(4500, table.Points[HandCategory.OnePair]);
        Assert.Equal(37500, table.Points[HandCategory.RoyalFlush]);
        Assert.Equal(Enum.GetValues<HandCategory>().Length, table.Points.Count);
    }

    [Fact]
    public void DefaultPointValues_MatchExpected()
    {
        var table = new PointTable();
        Assert.Equal(37500, table.Points[HandCategory.RoyalFlush]);
        Assert.Equal(28500, table.Points[HandCategory.StraightFlush]);
        Assert.Equal(22500, table.Points[HandCategory.FourOfAKind]);
        Assert.Equal(18700, table.Points[HandCategory.FullHouse]);
        Assert.Equal(15000, table.Points[HandCategory.Flush]);
        Assert.Equal(12000, table.Points[HandCategory.Straight]);
        Assert.Equal(9000, table.Points[HandCategory.ThreeOfAKind]);
        Assert.Equal(6700, table.Points[HandCategory.TwoPair]);
        Assert.Equal(4500, table.Points[HandCategory.OnePair]);
    }

    [Fact]
    public void GetPoints_ForHighCard_ReturnsHighCardValue()
    {
        var table = new PointTable();
        var result = new HandResult(HandCategory.HighCard, 14);
        Assert.Equal(14, table.GetPoints(result));
    }

    [Fact]
    public void GetPoints_ForNonHighCard_ReturnsTableValue()
    {
        var table = new PointTable();
        var result = new HandResult(HandCategory.FullHouse, 13);
        Assert.Equal(18700, table.GetPoints(result));
    }

    [Fact]
    public void CustomPointValues_Override()
    {
        var table = new PointTable();
        table.Points[HandCategory.OnePair] = 5000;
        var result = new HandResult(HandCategory.OnePair, 10);
        Assert.Equal(5000, table.GetPoints(result));
    }
}
