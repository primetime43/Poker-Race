using RacePoker.Core.Models;

namespace RacePoker.Core.Services;

public sealed class RaceHistoryService(string path)
{
    public RaceHistory Data { get; private set; } = new();
    public string? LastNotice { get; private set; }
    public bool HasPendingSave { get; private set; }

    public void Load()
    {
        Data = JsonFileStore.Load(path, () => new RaceHistory(), out var notice);
        LastNotice = notice;
        Data.Recent = (Data.Recent ?? new()).Where(Valid).DistinctBy(e => e.RaceId)
            .OrderByDescending(e => e.PlayedAt).Take(20).ToList();
        Data.TotalRaces = Math.Max(Data.Recent.Count, Data.TotalRaces);
        Data.Wins = Math.Clamp(Math.Max(Data.Wins, Data.Recent.Count(e => e.PlayerScore > e.ComputerScore)), 0, Data.TotalRaces);
        Data.Draws = Math.Clamp(Math.Max(Data.Draws, Data.Recent.Count(e => e.PlayerScore == e.ComputerScore)), 0, Data.TotalRaces - Data.Wins);
        Data.BestScore = Math.Max(Math.Max(0, Data.BestScore), Data.Recent.Select(e => e.PlayerScore).DefaultIfEmpty(0).Max());
    }

    public bool Record(RaceHistoryEntry entry)
    {
        if (!Valid(entry)) throw new ArgumentException("A completed race needs valid scores, rounds, difficulty, and an ID.");
        if (Data.Recent.Any(e => e.RaceId == entry.RaceId)) return false;
        Data.TotalRaces++;
        if (entry.PlayerScore > entry.ComputerScore) Data.Wins++;
        if (entry.PlayerScore == entry.ComputerScore) Data.Draws++;
        Data.BestScore = Math.Max(Data.BestScore, entry.PlayerScore);
        Data.Recent.Insert(0, entry);
        Data.Recent = Data.Recent.OrderByDescending(e => e.PlayedAt).Take(20).ToList();
        Save();
        return true;
    }

    public bool Save()
    {
        bool saved = JsonFileStore.Save(path, Data, out var notice);
        HasPendingSave = !saved;
        LastNotice = notice;
        return saved;
    }

    private static bool Valid(RaceHistoryEntry? entry) => entry is not null && entry.RaceId != Guid.Empty
        && entry.PlayerScore >= 0 && entry.ComputerScore >= 0 && entry.Rounds > 0 && Enum.IsDefined(entry.Difficulty);
}
