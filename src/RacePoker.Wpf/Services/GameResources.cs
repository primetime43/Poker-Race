using RacePoker.Core.Models;
using RacePoker.Core.Services;

namespace RacePoker.Wpf.Services;

public static class GameResources
{
    public static PointTable LoadPointTable()
    {
        using var stream = typeof(GameResources).Assembly.GetManifestResourceStream("RacePoker.PointTable.json")
            ?? throw new InvalidOperationException("The game's embedded point table is missing.");
        return PointTableService.Load(stream);
    }
}
