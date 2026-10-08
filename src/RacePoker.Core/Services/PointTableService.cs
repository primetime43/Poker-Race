using System.Text.Json;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;

namespace RacePoker.Core.Services;

public static class PointTableService
{
    public static PointTable Load(string path)
        => CreateTable(JsonFileStore.Load(path, () => new Dictionary<string, int>(), out _));

    public static PointTable Load(Stream stream)
        => CreateTable(JsonSerializer.Deserialize<Dictionary<string, int>>(stream) ?? new());

    private static PointTable CreateTable(Dictionary<string, int> values)
    {
        var table = new PointTable();
        foreach (var (key, value) in values)
            if (Enum.TryParse<HandCategory>(key, out var category) && Enum.IsDefined(category) && value >= 0 && value <= 1000000)
                table.Points[category] = value;
        return table;
    }
}
