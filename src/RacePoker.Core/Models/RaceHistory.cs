using RacePoker.Core.Enums;

namespace RacePoker.Core.Models;

public record RaceRunnerResult(string Name, int Score, bool IsComputer, bool IsPrimaryHuman);

public record RaceHistoryEntry(Guid RaceId, DateTimeOffset PlayedAt, int PlayerScore, int ComputerScore,
    int Rounds, ComputerDifficulty Difficulty, string RulesKey)
{
    public int HumanPlayers { get; init; } = 1;
    public bool HasComputer { get; init; } = true;
    public List<RaceRunnerResult> Racers { get; init; } = new();
    // Older solo records have no racer list and retain their original computer comparison.
    public int OpponentScore => Racers is { Count: > 0 }
        ? Racers.Where(r => !r.IsPrimaryHuman).Select(r => r.Score).DefaultIfEmpty(0).Max() : ComputerScore;
}

public class RaceHistory
{
    public int TotalRaces { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int BestScore { get; set; }
    public List<RaceHistoryEntry> Recent { get; set; } = new();
}
