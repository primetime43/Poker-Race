using RacePoker.Core.Enums;
using RacePoker.Core.Models;

namespace RacePoker.Core.Engine;

public static class HandEvaluator
{
    /// <summary>Original card positions that make the paid combination, excluding unused kickers.</summary>
    public static int[] GetScoringCardIndices(IReadOnlyList<Card> hand, HighCardRule highCardRule = HighCardRule.HighestRank)
    {
        var result = Evaluate(hand);
        if (result.Category == HandCategory.HighCard)
            return Enumerable.Range(0, hand.Count)
                .Where(i => highCardRule == HighCardRule.SumOfRanks || (int)hand[i].Rank == result.HighCard).ToArray();
        int groupSize = result.Category switch
        {
            HandCategory.OnePair or HandCategory.TwoPair => 2,
            HandCategory.ThreeOfAKind => 3,
            HandCategory.FourOfAKind => 4,
            _ => 0
        };
        if (groupSize == 0) return Enumerable.Range(0, hand.Count).ToArray();
        var counts = hand.GroupBy(c => c.Rank).ToDictionary(g => g.Key, g => g.Count());
        return Enumerable.Range(0, hand.Count).Where(i => counts[hand[i].Rank] == groupSize).ToArray();
    }

    public static HandResult Evaluate(IReadOnlyList<Card> hand)
    {
        if (hand.Count != 5)
            throw new ArgumentException("Hand must contain exactly 5 cards.");

        var values = hand.Select(c => (int)c.Rank).OrderBy(v => v).ToList();
        bool isFlush = hand.All(c => c.Suit == hand[0].Suit);
        bool isStraight = CheckStraight(values);

        int highCard = values[4];

        if (isStraight && isFlush && values[4] == 14 && values[0] == 10)
            return new HandResult(HandCategory.RoyalFlush, highCard);

        if (isStraight && isFlush)
            return new HandResult(HandCategory.StraightFlush, highCard);

        var groups = values.GroupBy(v => v).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();

        if (groups[0].Count() == 4)
            return new HandResult(HandCategory.FourOfAKind, groups[0].Key);

        if (groups[0].Count() == 3 && groups[1].Count() == 2)
            return new HandResult(HandCategory.FullHouse, groups[0].Key);

        if (isFlush)
            return new HandResult(HandCategory.Flush, highCard);

        if (isStraight)
            return new HandResult(HandCategory.Straight, highCard);

        if (groups[0].Count() == 3)
            return new HandResult(HandCategory.ThreeOfAKind, groups[0].Key);

        if (groups[0].Count() == 2 && groups[1].Count() == 2)
            return new HandResult(HandCategory.TwoPair, Math.Max(groups[0].Key, groups[1].Key));

        if (groups[0].Count() == 2)
            return new HandResult(HandCategory.OnePair, groups[0].Key);

        return new HandResult(HandCategory.HighCard, highCard);
    }

    private static bool CheckStraight(List<int> sortedValues)
    {
        // Ace-low straight: A-2-3-4-5
        if (sortedValues[0] == 2 && sortedValues[1] == 3 && sortedValues[2] == 4
            && sortedValues[3] == 5 && sortedValues[4] == 14)
            return true;

        // Normal consecutive check
        for (int i = 1; i < sortedValues.Count; i++)
        {
            if (sortedValues[i] != sortedValues[i - 1] + 1)
                return false;
        }

        return true;
    }
}
