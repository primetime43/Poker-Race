using RacePoker.Core.Engine;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;

namespace RacePoker.Tests;

public class HandEvaluatorTests
{
    private static Card C(Rank r, Suit s) => new(r, s);

    [Fact]
    public void RoyalFlush()
    {
        var hand = new[] { C(Rank.Ten, Suit.Spades), C(Rank.Jack, Suit.Spades), C(Rank.Queen, Suit.Spades), C(Rank.King, Suit.Spades), C(Rank.Ace, Suit.Spades) };
        var result = HandEvaluator.Evaluate(hand);
        Assert.Equal(HandCategory.RoyalFlush, result.Category);
    }

    [Fact]
    public void StraightFlush()
    {
        var hand = new[] { C(Rank.Five, Suit.Hearts), C(Rank.Six, Suit.Hearts), C(Rank.Seven, Suit.Hearts), C(Rank.Eight, Suit.Hearts), C(Rank.Nine, Suit.Hearts) };
        var result = HandEvaluator.Evaluate(hand);
        Assert.Equal(HandCategory.StraightFlush, result.Category);
    }

    [Fact]
    public void FourOfAKind()
    {
        var hand = new[] { C(Rank.Jack, Suit.Clubs), C(Rank.Jack, Suit.Diamonds), C(Rank.Jack, Suit.Hearts), C(Rank.Jack, Suit.Spades), C(Rank.Three, Suit.Hearts) };
        var result = HandEvaluator.Evaluate(hand);
        Assert.Equal(HandCategory.FourOfAKind, result.Category);
        Assert.Equal(11, result.HighCard);
    }

    [Fact]
    public void FullHouse()
    {
        var hand = new[] { C(Rank.King, Suit.Clubs), C(Rank.King, Suit.Diamonds), C(Rank.King, Suit.Hearts), C(Rank.Nine, Suit.Spades), C(Rank.Nine, Suit.Hearts) };
        var result = HandEvaluator.Evaluate(hand);
        Assert.Equal(HandCategory.FullHouse, result.Category);
    }

    [Fact]
    public void Flush()
    {
        var hand = new[] { C(Rank.Two, Suit.Diamonds), C(Rank.Five, Suit.Diamonds), C(Rank.Eight, Suit.Diamonds), C(Rank.Jack, Suit.Diamonds), C(Rank.Ace, Suit.Diamonds) };
        var result = HandEvaluator.Evaluate(hand);
        Assert.Equal(HandCategory.Flush, result.Category);
    }

    [Fact]
    public void Straight_AceHigh()
    {
        var hand = new[] { C(Rank.Ten, Suit.Clubs), C(Rank.Jack, Suit.Diamonds), C(Rank.Queen, Suit.Hearts), C(Rank.King, Suit.Spades), C(Rank.Ace, Suit.Clubs) };
        var result = HandEvaluator.Evaluate(hand);
        Assert.Equal(HandCategory.Straight, result.Category);
    }

    [Fact]
    public void Straight_AceLow()
    {
        var hand = new[] { C(Rank.Ace, Suit.Clubs), C(Rank.Two, Suit.Diamonds), C(Rank.Three, Suit.Hearts), C(Rank.Four, Suit.Spades), C(Rank.Five, Suit.Clubs) };
        var result = HandEvaluator.Evaluate(hand);
        Assert.Equal(HandCategory.Straight, result.Category);
    }

    [Fact]
    public void Straight_Middle()
    {
        var hand = new[] { C(Rank.Six, Suit.Clubs), C(Rank.Seven, Suit.Diamonds), C(Rank.Eight, Suit.Hearts), C(Rank.Nine, Suit.Spades), C(Rank.Ten, Suit.Clubs) };
        var result = HandEvaluator.Evaluate(hand);
        Assert.Equal(HandCategory.Straight, result.Category);
    }

    [Fact]
    public void ThreeOfAKind()
    {
        var hand = new[] { C(Rank.Seven, Suit.Clubs), C(Rank.Seven, Suit.Diamonds), C(Rank.Seven, Suit.Hearts), C(Rank.King, Suit.Spades), C(Rank.Two, Suit.Clubs) };
        var result = HandEvaluator.Evaluate(hand);
        Assert.Equal(HandCategory.ThreeOfAKind, result.Category);
    }

    [Fact]
    public void TwoPair()
    {
        var hand = new[] { C(Rank.Four, Suit.Clubs), C(Rank.Four, Suit.Diamonds), C(Rank.Nine, Suit.Hearts), C(Rank.Nine, Suit.Spades), C(Rank.Ace, Suit.Clubs) };
        var result = HandEvaluator.Evaluate(hand);
        Assert.Equal(HandCategory.TwoPair, result.Category);
    }

    [Fact]
    public void OnePair()
    {
        var hand = new[] { C(Rank.Queen, Suit.Clubs), C(Rank.Queen, Suit.Diamonds), C(Rank.Three, Suit.Hearts), C(Rank.Seven, Suit.Spades), C(Rank.Ten, Suit.Clubs) };
        var result = HandEvaluator.Evaluate(hand);
        Assert.Equal(HandCategory.OnePair, result.Category);
    }

    [Fact]
    public void HighCard()
    {
        var hand = new[] { C(Rank.Two, Suit.Clubs), C(Rank.Five, Suit.Diamonds), C(Rank.Eight, Suit.Hearts), C(Rank.Jack, Suit.Spades), C(Rank.Ace, Suit.Clubs) };
        var result = HandEvaluator.Evaluate(hand);
        Assert.Equal(HandCategory.HighCard, result.Category);
        Assert.Equal(14, result.HighCard);
    }

    [Fact]
    public void HighCard_ReturnsHighestValue()
    {
        var hand = new[] { C(Rank.Two, Suit.Clubs), C(Rank.Four, Suit.Diamonds), C(Rank.Six, Suit.Hearts), C(Rank.Eight, Suit.Spades), C(Rank.King, Suit.Clubs) };
        var result = HandEvaluator.Evaluate(hand);
        Assert.Equal(HandCategory.HighCard, result.Category);
        Assert.Equal(13, result.HighCard);
    }

    [Fact]
    public void ThrowsOnWrongHandSize()
    {
        var hand = new[] { C(Rank.Two, Suit.Clubs), C(Rank.Three, Suit.Clubs) };
        Assert.Throws<ArgumentException>(() => HandEvaluator.Evaluate(hand));
    }

    [Fact]
    public void AceLowStraightFlush()
    {
        var hand = new[] { C(Rank.Ace, Suit.Hearts), C(Rank.Two, Suit.Hearts), C(Rank.Three, Suit.Hearts), C(Rank.Four, Suit.Hearts), C(Rank.Five, Suit.Hearts) };
        var result = HandEvaluator.Evaluate(hand);
        Assert.Equal(HandCategory.StraightFlush, result.Category);
    }
}
