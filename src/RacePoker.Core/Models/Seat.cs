using RacePoker.Core.Enums;

namespace RacePoker.Core.Models;

public class Seat
{
    public int SeatId { get; }
    public SeatStatus Status { get; set; } = SeatStatus.Empty;
    public int Credits { get; set; }

    /// <summary>
    /// The 5 face-up cards the player picks from. Refreshes after each pick.
    /// </summary>
    public Card[] SelectionCards { get; set; } = Array.Empty<Card>();

    /// <summary>
    /// The player's hand, built up one card at a time (0 to 5 cards).
    /// </summary>
    public Card[] Hand { get; set; } = Array.Empty<Card>();

    public int TotalScore { get; set; }
    public int RoundPoints { get; set; }
    public string HandName { get; set; } = "";
    public bool IsHandComplete => Hand.Length >= 5;
    public bool IsReadyToContinue { get; set; }

    public Seat(int seatId)
    {
        SeatId = seatId;
    }

    public void ResetForRound()
    {
        SelectionCards = Array.Empty<Card>();
        Hand = Array.Empty<Card>();
        RoundPoints = 0;
        HandName = "";
        IsReadyToContinue = false;
    }

    public void ResetFull()
    {
        ResetForRound();
        TotalScore = 0;
        Status = SeatStatus.Empty;
        Credits = 0;
    }
}
