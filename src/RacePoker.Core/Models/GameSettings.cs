namespace RacePoker.Core.Models;

public class GameSettings
{
    public int MaxRounds { get; set; } = 10;
    public int FinishDistance { get; set; } = 150000;
    public int MaxSeats { get; set; } = 4;
    public double TurnDelaySeconds { get; set; } = 2.0;
    public double PickingTimeoutSeconds { get; set; } = 30.0;
    public double RoundEndTimeoutSeconds { get; set; } = 15.0;
    public double RacingDurationSeconds { get; set; } = 2.0;
    public double GameOverTimeoutSeconds { get; set; } = 30.0;
    public bool AutoAdvanceEnabled { get; set; } = true;
    public double AutoAdvanceDelaySeconds { get; set; } = 3.0;

    /// <summary>
    /// Race icon style: "Symbol" (unicode chess piece), "Image" (horse-racing.png).
    /// </summary>
    public string RaceIconStyle { get; set; } = "Image";
}
