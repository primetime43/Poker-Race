using RacePoker.Core.Enums;
using RacePoker.Core.Models;

namespace RacePoker.Core.Engine;

public static class ComputerStrategy
{
    public static int ChooseCard(IReadOnlyList<Card> hand, IReadOnlyList<Card> choices, ComputerDifficulty difficulty,
        PointTable table, IRng rng, HighCardRule highCardRule = HighCardRule.HighestRank)
    {
        if (hand.Count >= 5 || choices.Count != 5) throw new ArgumentException("Choose from five cards before the hand is complete.");
        if (!Enum.IsDefined(difficulty)) difficulty = ComputerDifficulty.Normal;
        if (difficulty == ComputerDifficulty.Easy) return rng.Next(choices.Count);
        if (difficulty == ComputerDifficulty.Normal || hand.Count == 4) return NormalPick(hand, choices, table, highCardRule);

        // Plan only from visible cards and a standard deck. Never inspect the engine's hidden deck or the player's cards.
        var known = hand.Concat(choices).ToHashSet();
        var unseen = Enum.GetValues<Suit>().SelectMany(s => Enum.GetValues<Rank>().Select(r => new Card(r, s)))
            .Where(c => !known.Contains(c)).ToArray();
        var totals = new double[choices.Count];
        const int samples = 64;
        for (int sample = 0; sample < samples; sample++)
        {
            var future = (Card[])unseen.Clone();
            for (int i = future.Length - 1; i > 0; i--)
            {
                int swap = rng.Next(i + 1);
                (future[i], future[swap]) = (future[swap], future[i]);
            }
            // The same possible future is used for each candidate to reduce sampling noise.
            for (int candidate = 0; candidate < choices.Count; candidate++)
            {
                var built = hand.Append(choices[candidate]).ToList();
                var row = choices.ToArray();
                int draw = 0;
                row[candidate] = future[draw++];
                while (built.Count < 5)
                {
                    int pick = NormalPick(built, row, table, highCardRule);
                    built.Add(row[pick]);
                    if (built.Count < 5) row[pick] = future[draw++];
                }
                totals[candidate] += Payout(built, table, highCardRule);
            }
        }
        return Enumerable.Range(0, choices.Count).OrderByDescending(i => totals[i])
            .ThenByDescending(i => PartialValue(hand, choices[i])).First();
    }

    private static int NormalPick(IReadOnlyList<Card> hand, IReadOnlyList<Card> choices, PointTable table, HighCardRule rule)
        => Enumerable.Range(0, choices.Count).OrderByDescending(i => hand.Count == 4
            ? Payout(hand.Append(choices[i]).ToArray(), table, rule) : PartialValue(hand, choices[i])).First();

    private static double PartialValue(IReadOnlyList<Card> hand, Card card)
        => hand.Count(c => c.Rank == card.Rank) * 100 + hand.Count(c => c.Suit == card.Suit) * 8 + (int)card.Rank;

    private static int Payout(IReadOnlyList<Card> hand, PointTable table, HighCardRule rule)
    {
        var result = HandEvaluator.Evaluate(hand);
        return result.Category == HandCategory.HighCard && rule == HighCardRule.SumOfRanks
            ? hand.Sum(c => (int)c.Rank) : table.GetPoints(result);
    }
}
