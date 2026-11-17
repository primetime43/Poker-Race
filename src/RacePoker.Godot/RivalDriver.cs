using RacePoker.Core.Engine;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;

namespace RacePoker.Godot;

/// <summary>A visible opponent using the same dealt cards, pick API, and payouts as the player.</summary>
public sealed class RivalDriver
{
    private double _wait;
    public int PicksMade { get; private set; }

    public void Tick(GameEngine engine, double delta)
    {
        if (engine.Phase != GamePhase.Picking || engine.ActiveSeatIndex != 0 || engine.IsTurnTransitioning)
        {
            _wait = 0;
            return;
        }
        _wait += delta;
        if (_wait < 0.6) return;
        _wait = 0;
        var seat = engine.Seats[0];
        int index = Enumerable.Range(0, seat.SelectionCards.Length)
            .OrderByDescending(i => Value(engine, seat.Hand, seat.SelectionCards[i]))
            .First();
        if (engine.PickCard(0, index)) PicksMade++;
    }

    private static double Value(GameEngine engine, Card[] hand, Card card)
    {
        if (hand.Length == 4)
        {
            var result = HandEvaluator.Evaluate(hand.Append(card).ToArray());
            return engine.PointTable.GetPoints(result);
        }
        // Build rank groups first, with a smaller preference for a possible flush.
        int matches = hand.Count(c => c.Rank == card.Rank);
        int suited = hand.Count(c => c.Suit == card.Suit);
        return matches * 100 + suited * 8 + (int)card.Rank;
    }
}
