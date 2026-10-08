using RacePoker.Core.Models;

namespace RacePoker.Core.Services;

public class SettingsService
{
    private readonly string _filePath;
    public string? LastNotice { get; private set; }

    public SettingsService(string? filePath = null)
        => _filePath = filePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RacePoker", "settings.json");

    public GameSettings Load()
    {
        var settings = JsonFileStore.Load(_filePath, () => new GameSettings(), out var notice);
        settings.Normalize();
        LastNotice = notice;
        return settings;
    }

    public bool Save(GameSettings settings)
    {
        settings.Normalize();
        var saved = JsonFileStore.Save(_filePath, settings, out var notice);
        LastNotice = notice;
        return saved;
    }
}
