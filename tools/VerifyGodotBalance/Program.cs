using System.Diagnostics;
using RacePoker.Core.Engine;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;
using RacePoker.Core.Services;

// Reproducible diagnostic, not a win-rate claim: each level plays the same 100
// seeded solo decks using the actual engine and its replacement-card rules.
const int races = 100;
Console.WriteLine("Difficulty,Races,MeanScore,MeanRounds,FinishPercent,MeanDecisionMs,MaxDecisionMs");
foreach (var difficulty in Enum.GetValues<ComputerDifficulty>())
{
    long scores = 0, rounds = 0, decisions = 0, elapsed = 0, maximum = 0;
    int finishes = 0;
    for (int seed = 0; seed < races; seed++)
    {
        var engine = new GameEngine(new GameSettings
        {
            MaxSeats = 1, MaxRounds = 10, FinishDistance = 150000,
            PickingTimeoutSeconds = 0, TurnDelaySeconds = 0, RacingDurationSeconds = 0,
            AutoAdvanceEnabled = true, AutoAdvanceDelaySeconds = 1
        }, rng: new SeededRng(seed), highScores: new HighScoreService(Path.Combine(Path.GetTempPath(), "RacePoker-balance-unused.json")));
        var strategyRng = new SeededRng(seed + 10000);
        engine.JoinSeat(0);
        engine.PressDeal(0);
        int steps = 0;
        while (engine.Phase != GamePhase.GameOver)
        {
            if (++steps > 1000) throw new InvalidOperationException("Race stalled.");
            if (engine.Phase == GamePhase.Picking && !engine.IsTurnTransitioning)
            {
                var seat = engine.Seats[0];
                long start = Stopwatch.GetTimestamp();
                int pick = ComputerStrategy.ChooseCard(seat.Hand, seat.SelectionCards, difficulty, engine.PointTable, strategyRng);
                long duration = Stopwatch.GetTimestamp() - start;
                elapsed += duration;
                maximum = Math.Max(maximum, duration);
                decisions++;
                if (!engine.PickCard(0, pick)) throw new InvalidOperationException("Illegal strategy pick.");
            }
            else engine.Tick(TimeSpan.FromSeconds(1));
        }
        scores += engine.Seats[0].TotalScore;
        rounds += engine.Round;
        if (engine.Seats[0].TotalScore >= 150000) finishes++;
    }
    Console.WriteLine(FormattableString.Invariant($"{difficulty},{races},{scores / (double)races:F0},{rounds / (double)races:F2},{finishes},{elapsed * 1000.0 / Stopwatch.Frequency / decisions:F3},{maximum * 1000.0 / Stopwatch.Frequency:F3}"));
}

sealed class SeededRng(int seed) : IRng
{
    private readonly Random _random = new(seed);
    public int Next(int maxExclusive) => _random.Next(maxExclusive);
}
