using RacePoker.Core.Engine;
using RacePoker.Core.Enums;

namespace RacePoker.Core.Models;

public class Deck
{
    private readonly IRng _rng;
    private readonly List<Card> _cards = new(52);

    public int Remaining => _cards.Count;

    public Deck(IRng rng)
    {
        _rng = rng;
        Shuffle();
    }

    public void Shuffle()
    {
        _cards.Clear();
        foreach (var suit in Enum.GetValues<Suit>())
            foreach (var rank in Enum.GetValues<Rank>())
                _cards.Add(new Card(rank, suit));

        // Fisher-Yates shuffle
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    public Card[] Draw(int count)
    {
        if (count > _cards.Count)
            throw new InvalidOperationException($"Cannot draw {count} cards, only {_cards.Count} remaining.");

        var drawn = _cards.GetRange(_cards.Count - count, count).ToArray();
        _cards.RemoveRange(_cards.Count - count, count);
        return drawn;
    }
}
