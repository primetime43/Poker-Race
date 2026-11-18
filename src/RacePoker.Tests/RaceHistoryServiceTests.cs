using RacePoker.Core.Enums;
using RacePoker.Core.Models;
using RacePoker.Core.Services;

namespace RacePoker.Tests;

public class RaceHistoryServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "RacePoker-History-" + Guid.NewGuid());
    private string HistoryPath => Path.Combine(_directory, "history.json");
    public RaceHistoryServiceTests() => Directory.CreateDirectory(_directory);
    private static RaceHistoryEntry Entry(int player, int computer, int offset = 0) => new(Guid.NewGuid(),
        DateTimeOffset.UtcNow.AddMinutes(offset), player, computer, 10, ComputerDifficulty.Normal, "test-rules");

    [Fact]
    public void CountsWinsDrawsAndBestScoreAcrossReloadsWithoutDuplicateResults()
    {
        var service = new RaceHistoryService(HistoryPath);
        service.Load();
        var win = Entry(100, 50);
        Assert.True(service.Record(win));
        Assert.False(service.Record(win));
        service.Record(Entry(200, 300));
        service.Record(Entry(150, 150));
        var reloaded = new RaceHistoryService(HistoryPath);
        reloaded.Load();
        Assert.Equal(3, reloaded.Data.TotalRaces);
        Assert.Equal(1, reloaded.Data.Wins);
        Assert.Equal(1, reloaded.Data.Draws);
        Assert.Equal(200, reloaded.Data.BestScore);
        Assert.Equal(3, reloaded.Data.Recent.Count);
    }

    [Fact]
    public void RetainsTwentyRecentRacesAndLifetimeTotals()
    {
        var service = new RaceHistoryService(HistoryPath);
        for (int i = 1; i <= 25; i++) service.Record(Entry(i * 100, 0, i));
        var reloaded = new RaceHistoryService(HistoryPath);
        reloaded.Load();
        Assert.Equal(25, reloaded.Data.TotalRaces);
        Assert.Equal(25, reloaded.Data.Wins);
        Assert.Equal(2500, reloaded.Data.BestScore);
        Assert.Equal(20, reloaded.Data.Recent.Count);
        Assert.Equal(2500, reloaded.Data.Recent[0].PlayerScore);
        Assert.Equal(600, reloaded.Data.Recent[^1].PlayerScore);
    }

    [Fact]
    public void RecoversCorruptHistoryFromBackupAndPreservesDamagedFile()
    {
        var service = new RaceHistoryService(HistoryPath);
        service.Record(Entry(100, 50));
        service.Record(Entry(200, 50));
        File.WriteAllText(HistoryPath, "{ broken");
        var reloaded = new RaceHistoryService(HistoryPath);
        reloaded.Load();
        Assert.Equal(1, reloaded.Data.TotalRaces);
        Assert.Contains("Recovered", reloaded.LastNotice);
        Assert.Single(Directory.GetFiles(_directory, "*.corrupt-*"));
    }

    [Fact]
    public void FailedSaveKeepsSessionResultAndCanRetryWithoutDoubleCounting()
    {
        string blocked = Path.Combine(_directory, "blocked");
        Directory.CreateDirectory(blocked);
        var service = new RaceHistoryService(blocked);
        service.Record(Entry(100, 50));
        Assert.True(service.HasPendingSave);
        Assert.Equal(1, service.Data.TotalRaces);
        Directory.Delete(blocked);
        Assert.True(service.Save());
        Assert.False(service.HasPendingSave);
        var reloaded = new RaceHistoryService(blocked);
        reloaded.Load();
        Assert.Equal(1, reloaded.Data.TotalRaces);
    }

    [Fact]
    public void MultiplayerHistoryComparesPlayerOneWithAllOtherRacersAndKeepsTheirScores()
    {
        var service = new RaceHistoryService(HistoryPath);
        var race = Entry(100, 50) with
        {
            HumanPlayers = 2, HasComputer = true,
            Racers = new()
            {
                new("PLAYER 1", 100, false, true), new("PLAYER 2", 300, false, false), new("COMPUTER", 50, true, false)
            }
        };
        service.Record(race);
        Assert.Equal(0, service.Data.Wins); // Beating the computer alone is not a multiplayer win.
        Assert.Equal(300, race.OpponentScore);
        var tied = race with
        {
            RaceId = Guid.NewGuid(), PlayerScore = 300,
            Racers = new() { new("PLAYER 1", 300, false, true), new("PLAYER 2", 300, false, false), new("COMPUTER", 50, true, false) }
        };
        service.Record(tied);
        var loaded = new RaceHistoryService(HistoryPath);
        loaded.Load();
        Assert.Equal(2, loaded.Data.TotalRaces);
        Assert.Equal(0, loaded.Data.Wins);
        Assert.Equal(1, loaded.Data.Draws);
        Assert.All(loaded.Data.Recent, result => Assert.Equal(3, result.Racers.Count));
        Assert.Equal(300, loaded.Data.BestScore);
    }

    public void Dispose()
    {
        foreach (string file in Directory.EnumerateFiles(_directory)) File.Delete(file);
        Directory.Delete(_directory);
    }
}
