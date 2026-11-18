using RacePoker.Core.Enums;

namespace RacePoker.Core.Models;

public record RaceHistoryEntry(Guid RaceId, DateTimeOffset PlayedAt, int PlayerScore, int ComputerScore,
    int Rounds, ComputerDifficulty Difficulty, string RulesKey);

public class RaceHistory
{
    public int TotalRaces { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int BestScore { get; set; }
    public List<RaceHistoryEntry> Recent { get; set; } = new();
}
