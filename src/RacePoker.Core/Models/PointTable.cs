using RacePoker.Core.Enums;

namespace RacePoker.Core.Models;

public class PointTable
{
    public Dictionary<HandCategory, int> Points { get; set; } = new()
    {
        [HandCategory.RoyalFlush] = 37500,
        [HandCategory.StraightFlush] = 28500,
        [HandCategory.FourOfAKind] = 22500,
        [HandCategory.FullHouse] = 18700,
        [HandCategory.Flush] = 15000,
        [HandCategory.Straight] = 12000,
        [HandCategory.ThreeOfAKind] = 9000,
        [HandCategory.TwoPair] = 6700,
        [HandCategory.OnePair] = 4500,
        [HandCategory.HighCard] = 0
    };

    public int GetPoints(HandResult result)
    {
        if (result.Category == HandCategory.HighCard)
            return result.HighCard;

        return Points.TryGetValue(result.Category, out var pts) ? pts : 0;
    }
}
