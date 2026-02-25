using System.Text.Json;
using RacePoker.Core.Models;

namespace RacePoker.Core.Services;

public class HighScoreService
{
    private readonly string _filePath;
    private const int MaxEntries = 10;

    public List<HighScoreEntry> Entries { get; private set; } = new();

    public HighScoreService(string? filePath = null)
    {
        _filePath = filePath ?? GetDefaultPath();
    }

    private static string GetDefaultPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "RacePoker");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "highscores.json");
    }

    public void Load()
    {
        if (!File.Exists(_filePath))
        {
            Entries = new List<HighScoreEntry>();
            return;
        }

        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            Entries = new List<HighScoreEntry>();
            return;
        }

        Entries = JsonSerializer.Deserialize<List<HighScoreEntry>>(json) ?? new List<HighScoreEntry>();
        Entries = Entries.OrderByDescending(e => e.Score).Take(MaxEntries).ToList();
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(Entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    public bool TryAdd(HighScoreEntry entry)
    {
        if (Entries.Count >= MaxEntries && entry.Score <= Entries[^1].Score)
            return false;

        Entries.Add(entry);
        Entries = Entries.OrderByDescending(e => e.Score).Take(MaxEntries).ToList();
        Save();
        return true;
    }

    public bool QualifiesForHighScore(int score)
    {
        return Entries.Count < MaxEntries || score > Entries[^1].Score;
    }
}
