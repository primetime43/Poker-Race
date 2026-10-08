using System.Globalization;
using RacePoker.Core.Enums;

namespace RacePoker.Core.Models;

public static class ScoreRules
{
    public static string Key(GameSettings settings, PointTable table, int players)
    {
        var payouts = string.Join(",", table.Points.OrderBy(p => p.Key).Select(p => $"{(int)p.Key}:{p.Value}"));
        return FormattableString.Invariant($"fixed-v2|rounds={settings.MaxRounds}|finish={settings.FinishDistance}|end={(int)settings.FinishRule}|high={(int)settings.HighCardRule}|seconds={settings.PickingTimeoutSeconds:R}|players={players}|{payouts}");
    }

    public static string Describe(GameSettings settings, int players)
        => $"{players}P · {settings.MaxRounds} rounds · {settings.FinishDistance:N0} pts · "
           + (settings.PickingTimeoutSeconds == 0 ? "untimed" : $"{settings.PickingTimeoutSeconds:0}s")
           + (settings.FinishRule == FinishRule.FirstToFinish ? " · first finish" : " · full round")
           + (settings.HighCardRule == HighCardRule.HighestRank ? " · highest rank" : " · rank sum");
}
