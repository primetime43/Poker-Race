namespace RacePoker.Wpf.ViewModels;

public class InitialsEntryViewModel : ViewModelBase
{
    private string _initials;
    private bool _replaceOnNextInput = true;
    private bool _isSelected;
    public int SeatId { get; }
    public string PlayerLabel { get; }
    public int Score { get; }
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

    public string Initials
    {
        get => _initials;
        set
        {
            var cleaned = new string((value ?? "").ToUpperInvariant().Where(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9').Take(3).ToArray());
            _replaceOnNextInput = false;
            SetProperty(ref _initials, cleaned);
        }
    }

    public void Append(string character)
    {
        var text = _replaceOnNextInput ? "" : Initials;
        if (text.Length < 3) Initials = text + character;
    }
    public void Backspace() => Initials = _replaceOnNextInput || Initials.Length == 0 ? "" : Initials[..^1];

    public InitialsEntryViewModel(int seatId, int score)
    {
        SeatId = seatId;
        Score = score;
        PlayerLabel = $"Player {seatId + 1}";
        _initials = $"P{seatId + 1}";
    }
}
