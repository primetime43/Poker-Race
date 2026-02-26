namespace RacePoker.Wpf.ViewModels;

public class InitialsEntryViewModel : ViewModelBase
{
    private string _initials = "";

    public int SeatId { get; }
    public string PlayerLabel { get; }
    public int Score { get; }

    public string Initials
    {
        get => _initials;
        set
        {
            var upper = (value ?? "").ToUpperInvariant();
            if (upper.Length > 3)
                upper = upper[..3];
            SetProperty(ref _initials, upper);
        }
    }

    public InitialsEntryViewModel(int seatId, int score)
    {
        SeatId = seatId;
        Score = score;
        PlayerLabel = $"Player {seatId + 1}";
        _initials = $"P{seatId + 1}";
    }
}
