using RacePoker.Core.Engine;
using RacePoker.Core.Models;

namespace RacePoker.Tests;

public class SeededRng : IRng
{
    private readonly Random _random;
    public SeededRng(int seed) => _random = new Random(seed);
    public int Next(int maxExclusive) => _random.Next(maxExclusive);
}

public class DeckTests
{
    [Fact]
    public void DeckContains52UniqueCards()
    {
        var deck = new Deck(new SeededRng(42));
        var cards = deck.Draw(52);
        Assert.Equal(52, cards.Length);
        Assert.Equal(52, cards.Distinct().Count());
    }

    [Fact]
    public void DrawRemovesCardsFromDeck()
    {
        var deck = new Deck(new SeededRng(42));
        deck.Draw(5);
        Assert.Equal(47, deck.Remaining);
    }

    [Fact]
    public void DrawTooManyThrows()
    {
        var deck = new Deck(new SeededRng(42));
        deck.Draw(52);
        Assert.Throws<InvalidOperationException>(() => deck.Draw(1));
    }

    [Fact]
    public void ShuffleResetsDeck()
    {
        var deck = new Deck(new SeededRng(42));
        deck.Draw(30);
        Assert.Equal(22, deck.Remaining);
        deck.Shuffle();
        Assert.Equal(52, deck.Remaining);
    }

    [Fact]
    public void DeterministicShuffleWithSameRng()
    {
        var deck1 = new Deck(new SeededRng(123));
        var deck2 = new Deck(new SeededRng(123));

        var hand1 = deck1.Draw(5);
        var hand2 = deck2.Draw(5);

        Assert.Equal(hand1, hand2);
    }

    [Fact]
    public void DifferentSeedsProduceDifferentOrders()
    {
        var deck1 = new Deck(new SeededRng(1));
        var deck2 = new Deck(new SeededRng(2));

        var hand1 = deck1.Draw(5);
        var hand2 = deck2.Draw(5);

        // Extremely unlikely these would be identical
        Assert.NotEqual(hand1, hand2);
    }
}
