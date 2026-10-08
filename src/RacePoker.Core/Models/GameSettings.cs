using RacePoker.Core.Enums;

namespace RacePoker.Core.Models;

public class GameSettings
{
    public FinishRule FinishRule { get; set; } = FinishRule.EndOfRound;
    public HighCardRule HighCardRule { get; set; } = HighCardRule.HighestRank;

    public int MaxRounds { get; set; } = 10;
    public int FinishDistance { get; set; } = 150000;
    public int MaxSeats { get; set; } = 4;
    public double TurnDelaySeconds { get; set; } = 2.0;
    public double PickingTimeoutSeconds { get; set; } = 30.0;
    // Retained for old settings files; manual rounds now wait for Continue.
    public double RoundEndTimeoutSeconds { get; set; } = 15.0;
    public double RacingDurationSeconds { get; set; } = 2.0;
    public double GameOverTimeoutSeconds { get; set; } = 30.0;
    public bool AutoAdvanceEnabled { get; set; } = true;
    public double AutoAdvanceDelaySeconds { get; set; } = 3.0;
    public bool SoundEnabled { get; set; } = true;

    /// <summary>
    /// Race icon style: "Symbol" (unicode chess piece), "Image" (animated horse and jockey).
    /// </summary>
    public string RaceIconStyle { get; set; } = "Image";

    /// <summary>
    /// Hand result decoration: "Plain", "Marquee", or "Lights".
    /// </summary>
    public string HandResultStyle { get; set; } = "Marquee";

    /// <summary>Card fan sway range in degrees (±). 0 = no sway.</summary>
    public double CardSwayDegrees { get; set; } = 10;

    /// <summary>Logo glow max blur radius. 0 = no glow.</summary>
    public double LogoGlowBlur { get; set; } = 30;

    /// <summary>Keep persisted or edited values within safe, supported ranges.</summary>
    public void Normalize()
    {
        MaxRounds = Math.Clamp(MaxRounds, 1, 25);
        FinishDistance = Math.Clamp(FinishDistance, 1000, 1000000);
        MaxSeats = Math.Clamp(MaxSeats, 1, 4);
        TurnDelaySeconds = Valid(TurnDelaySeconds, 0, 10, 2);
        PickingTimeoutSeconds = Valid(PickingTimeoutSeconds, 0, 60, 30);
        RacingDurationSeconds = Valid(RacingDurationSeconds, 0, 10, 2);
        GameOverTimeoutSeconds = Valid(GameOverTimeoutSeconds, 5, 300, 30);
        AutoAdvanceDelaySeconds = Valid(AutoAdvanceDelaySeconds, 1, 10, 3);
        CardSwayDegrees = Valid(CardSwayDegrees, 0, 25, 10);
        LogoGlowBlur = Valid(LogoGlowBlur, 0, 50, 30);
        if (!Enum.IsDefined(FinishRule)) FinishRule = FinishRule.EndOfRound;
        if (!Enum.IsDefined(HighCardRule)) HighCardRule = HighCardRule.HighestRank;
        if (RaceIconStyle is not ("Image" or "Symbol")) RaceIconStyle = "Image";
        if (HandResultStyle is not ("Plain" or "Marquee" or "Lights")) HandResultStyle = "Marquee";
    }

    private static double Valid(double value, double min, double max, double fallback)
        => double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;

}
