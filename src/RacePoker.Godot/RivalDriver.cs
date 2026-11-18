using RacePoker.Core.Engine;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;

namespace RacePoker.Godot;

/// <summary>A visible opponent using the same dealt cards, pick API, and payouts as the player.</summary>
public sealed class RivalDriver(ComputerDifficulty difficulty = ComputerDifficulty.Normal, IRng? rng = null)
{
    private readonly IRng _rng = rng ?? new SystemRng();
    public ComputerDifficulty Difficulty { get; } = difficulty;
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
        int index = ComputerStrategy.ChooseCard(seat.Hand, seat.SelectionCards, Difficulty, engine.PointTable, _rng, engine.Settings.HighCardRule);
        if (engine.PickCard(0, index)) PicksMade++;
    }

}
