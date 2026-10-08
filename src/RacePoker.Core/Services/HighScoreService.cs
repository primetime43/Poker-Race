using RacePoker.Core.Models;

namespace RacePoker.Core.Services;

public class HighScoreService
{
    private readonly string _filePath;
    private const int MaxEntries = 10;
    public List<HighScoreEntry> Entries { get; private set; } = new();
    public string? LastNotice { get; private set; }
    public bool HasPendingSave { get; private set; }

    public HighScoreService(string? filePath = null)
        => _filePath = filePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RacePoker", "highscores.json");

    public void Load()
    {
        var entries = JsonFileStore.Load(_filePath, () => new List<HighScoreEntry>(), out var notice);
        LastNotice = notice;
        Entries = Clean(entries);
    }

    public bool Save()
    {
        Entries = Clean(Entries);
        var saved = JsonFileStore.Save(_filePath, Entries, out var notice);
        LastNotice = notice;
        HasPendingSave = !saved;
        return saved;
    }

    public bool RetrySave() => !HasPendingSave || Save();

    public IReadOnlyList<HighScoreEntry> GetEntries(string rulesKey = "legacy")
        => Entries.Where(e => e.RulesKey == rulesKey).OrderByDescending(e => e.Score).Take(MaxEntries).ToArray();

    public bool QualifiesForHighScore(int score, string rulesKey = "legacy")
    {
        var board = GetEntries(rulesKey);
        return score >= 0 && (board.Count < MaxEntries || score > board[^1].Score);
    }

    public bool TryAdd(HighScoreEntry entry)
    {
        Normalize(entry);
        if (!QualifiesForHighScore(entry.Score, entry.RulesKey)) return false;
        Entries.Add(entry);
        Save();
        return true;
    }

    private static List<HighScoreEntry> Clean(IEnumerable<HighScoreEntry> entries)
    {
        var valid = entries.Where(e => e is not null && e.Score >= 0).ToList();
        foreach (var entry in valid) Normalize(entry);
        return valid.GroupBy(e => e.RulesKey).SelectMany(g => g.OrderByDescending(e => e.Score).Take(MaxEntries)).ToList();
    }

    private static void Normalize(HighScoreEntry entry)
    {
        entry.Initials = new string((entry.Initials ?? "").Where(char.IsLetterOrDigit).Take(3).ToArray()).ToUpperInvariant();
        if (entry.Initials.Length == 0) entry.Initials = "AAA";
        if (string.IsNullOrWhiteSpace(entry.RulesKey)) entry.RulesKey = "legacy";
        if (string.IsNullOrWhiteSpace(entry.RulesDescription)) entry.RulesDescription = "Legacy records (rules unknown)";
    }
}
