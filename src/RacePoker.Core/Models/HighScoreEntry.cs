namespace RacePoker.Core.Models;

public class HighScoreEntry
{
    // Missing metadata identifies an older, unverified scoring profile.
    public string RulesKey { get; set; } = "legacy";
    public string RulesDescription { get; set; } = "Legacy records (rules unknown)";
    public string Initials { get; set; } = "AAA";
    public int Score { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
}
