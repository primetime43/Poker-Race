using RacePoker.Core.Engine;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;
using RacePoker.Core.Services;

namespace RacePoker.Tests;

public class ComputerStrategyTests
{
    private static Card C(int rank, Suit suit) => new((Rank)rank, suit);
    private sealed class FixedRng(int value) : IRng { public int Next(int maxExclusive) => value % maxExclusive; }
    private sealed class SeededStrategyRng(int seed) : IRng
    {
        private readonly Random _random = new(seed);
        public int Next(int maxExclusive) => _random.Next(maxExclusive);
    }

    [Fact]
    public void Easy_UsesRandomChoiceInsteadOfOptimizingTheHand()
    {
        Card[] hand = { C(2, Suit.Hearts) };
        Card[] choices = { C(2, Suit.Clubs), C(4, Suit.Clubs), C(8, Suit.Spades), C(9, Suit.Diamonds), C(14, Suit.Clubs) };
        Assert.Equal(4, ComputerStrategy.ChooseCard(hand, choices, ComputerDifficulty.Easy, new PointTable(), new FixedRng(4)));
        Assert.Equal(0, ComputerStrategy.ChooseCard(hand, choices, ComputerDifficulty.Normal, new PointTable(), new FixedRng(4)));
    }

    [Fact]
    public void Hard_PlansAFlushOverANearTermPairWithoutChangingVisibleCards()
    {
        Card[] hand = { C(2, Suit.Hearts), C(7, Suit.Hearts), C(11, Suit.Hearts) };
        Card[] choices = { C(2, Suit.Clubs), C(9, Suit.Hearts), C(13, Suit.Hearts), C(4, Suit.Diamonds), C(12, Suit.Clubs) };
        var originalHand = hand.ToArray();
        var originalChoices = choices.ToArray();
        Assert.Equal(0, ComputerStrategy.ChooseCard(hand, choices, ComputerDifficulty.Normal, new PointTable(), new FixedRng(0)));
        int hard = ComputerStrategy.ChooseCard(hand, choices, ComputerDifficulty.Hard, new PointTable(), new SeededStrategyRng(42));
        Assert.Contains(hard, new[] { 1, 2 });
        Assert.Equal(originalHand, hand);
        Assert.Equal(originalChoices, choices);
        Assert.Equal(hard, ComputerStrategy.ChooseCard(hand, choices, ComputerDifficulty.Hard, new PointTable(), new SeededStrategyRng(42)));
    }

    [Theory]
    [InlineData(ComputerDifficulty.Normal)]
    [InlineData(ComputerDifficulty.Hard)]
    public void FinalPick_UsesTheActualConfiguredPayout(ComputerDifficulty difficulty)
    {
        Card[] hand = { C(2, Suit.Clubs), C(6, Suit.Diamonds), C(8, Suit.Spades), C(10, Suit.Hearts) };
        Card[] choices = { C(2, Suit.Hearts), C(2, Suit.Diamonds), C(4, Suit.Clubs), C(5, Suit.Spades), C(14, Suit.Clubs) };
        var table = new PointTable();
        Assert.Equal(0, ComputerStrategy.ChooseCard(hand, choices, difficulty, table, new FixedRng(0)));
        table.Points[HandCategory.OnePair] = 0;
        Assert.Equal(4, ComputerStrategy.ChooseCard(hand, choices, difficulty, table, new FixedRng(0)));
    }

    [Fact]
    public void RejectsCompletedHandsAndIncompleteChoiceRows()
    {
        Card[] cards = { C(2, Suit.Clubs), C(3, Suit.Diamonds), C(4, Suit.Spades), C(5, Suit.Hearts), C(6, Suit.Clubs) };
        Assert.Throws<ArgumentException>(() => ComputerStrategy.ChooseCard(cards, cards, ComputerDifficulty.Hard, new PointTable(), new FixedRng(0)));
        Assert.Throws<ArgumentException>(() => ComputerStrategy.ChooseCard(Array.Empty<Card>(), cards.Take(4).ToArray(), ComputerDifficulty.Easy, new PointTable(), new FixedRng(0)));
    }

    [Theory]
    [InlineData(ComputerDifficulty.Easy)]
    [InlineData(ComputerDifficulty.Normal)]
    [InlineData(ComputerDifficulty.Hard)]
    public void EveryLevelBuildsALegalScoredHandThroughTheGameEngine(ComputerDifficulty difficulty)
    {
        var engine = new GameEngine(new GameSettings { MaxSeats = 1 }, rng: new SeededStrategyRng(12),
            highScores: new HighScoreService(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json")));
        engine.JoinSeat(0); engine.PressDeal(0);
        var strategyRng = new SeededStrategyRng(42);
        for (int i = 0; i < 5; i++)
        {
            var seat = engine.Seats[0];
            int index = ComputerStrategy.ChooseCard(seat.Hand, seat.SelectionCards, difficulty, engine.PointTable, strategyRng);
            Assert.InRange(index, 0, 4);
            Assert.True(engine.PickCard(0, index));
        }
        Assert.Equal(5, engine.Seats[0].Hand.Distinct().Count());
        Assert.True(engine.Seats[0].RoundPoints > 0);
    }
}
