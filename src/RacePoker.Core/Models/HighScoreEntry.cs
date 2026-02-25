namespace RacePoker.Core.Models;

public class HighScoreEntry
{
    public string Initials { get; set; } = "AAA";
    public int Score { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
}
