using System.Text.Json;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;

namespace RacePoker.Core.Services;

public static class PointTableService
{
    public static PointTable Load(string path)
    {
        if (!File.Exists(path))
            return new PointTable();

        var json = File.ReadAllText(path);
        var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
        if (dict is null)
            return new PointTable();

        var table = new PointTable();
        foreach (var kvp in dict)
        {
            if (Enum.TryParse<HandCategory>(kvp.Key, out var category))
                table.Points[category] = kvp.Value;
        }

        return table;
    }
}
