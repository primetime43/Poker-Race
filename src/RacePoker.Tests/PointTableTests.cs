using RacePoker.Core.Enums;
using RacePoker.Core.Models;

namespace RacePoker.Tests;

public class PointTableTests
{
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
