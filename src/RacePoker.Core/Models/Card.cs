using RacePoker.Core.Enums;

namespace RacePoker.Core.Models;

public record Card(Rank Rank, Suit Suit)
{
    public string ImageFileName => $"{RankName}_of_{Suit.ToString().ToLower()}.png";

    private string RankName => Rank switch
    {
        Rank.Two => "2",
        Rank.Three => "3",
        Rank.Four => "4",
        Rank.Five => "5",
        Rank.Six => "6",
        Rank.Seven => "7",
        Rank.Eight => "8",
        Rank.Nine => "9",
        Rank.Ten => "10",
        Rank.Jack => "jack",
        Rank.Queen => "queen",
        Rank.King => "king",
        Rank.Ace => "ace",
        _ => throw new ArgumentOutOfRangeException()
    };
}
