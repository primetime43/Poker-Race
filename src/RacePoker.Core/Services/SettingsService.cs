using System.Text.Json;
using RacePoker.Core.Models;

namespace RacePoker.Core.Services;

public class SettingsService
{
    private readonly string _filePath;

    public SettingsService(string? filePath = null)
    {
        _filePath = filePath ?? GetDefaultPath();
    }

    private static string GetDefaultPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "RacePoker");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "settings.json");
    }

    public GameSettings Load()
    {
        if (!File.Exists(_filePath))
            return new GameSettings();

        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
            return new GameSettings();

        return JsonSerializer.Deserialize<GameSettings>(json) ?? new GameSettings();
    }

    public void Save(GameSettings settings)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
