using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;

namespace RacePoker.Wpf.ViewModels;

public class SeatViewModel : ViewModelBase
{
    private readonly Action<int> _joinAction;
    private readonly Action<int> _dealAction;
    private readonly Action<int> _continueAction;
    private readonly Action<int, int> _pickCardAction;

    private SeatStatus _status;
    private int _totalScore;
    private int _trackScore;
    private int _roundPoints;
    private string _handName = "";
    private GamePhase _currentPhase;
    private bool _isActiveSeat;
    private bool _isTurnTransitioning;

    public int SeatId { get; }
    public string SeatLabel => $"Player {SeatId + 1}";

    public bool IsActiveSeat
    {
        get => _isActiveSeat;
        set
        {
            if (SetProperty(ref _isActiveSeat, value))
                UpdateSelectionCanPick();
        }
    }

    public bool IsTurnTransitioning
    {
        get => _isTurnTransitioning;
        set
        {
            if (SetProperty(ref _isTurnTransitioning, value))
                UpdateSelectionCanPick();
        }
    }

    public Brush SeatColorBrush => SeatId switch
    {
        0 => new SolidColorBrush(Color.FromRgb(0xFF, 0x44, 0x44)),
        1 => new SolidColorBrush(Color.FromRgb(0x44, 0x88, 0xFF)),
        2 => new SolidColorBrush(Color.FromRgb(0x44, 0xDD, 0x44)),
        3 => new SolidColorBrush(Color.FromRgb(0xFF, 0xDD, 0x44)),
        _ => Brushes.White
    };

    /// <summary>
    /// The 5 face-up selection cards the player picks from.
    /// </summary>
    public ObservableCollection<CardViewModel> SelectionCards { get; } = new();

    /// <summary>
    /// The player's hand (up to 5 cards, built one at a time).
    /// </summary>
    public ObservableCollection<CardViewModel> Cards { get; } = new();

    public SeatStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(IsJoined));
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(CanJoin));
                OnPropertyChanged(nameof(CanDeal));
            }
        }
    }

    public int TotalScore
    {
        get => _totalScore;
        set => SetProperty(ref _totalScore, value);
    }

    /// <summary>
    /// Score used for horse position on track. During Racing this interpolates;
    /// otherwise it equals TotalScore.
    /// </summary>
    public int TrackScore
    {
        get => _trackScore;
        set => SetProperty(ref _trackScore, value);
    }

    public int RoundPoints
    {
        get => _roundPoints;
        set => SetProperty(ref _roundPoints, value);
    }

    public string HandName
    {
        get => _handName;
        set => SetProperty(ref _handName, value);
    }

    public GamePhase CurrentPhase
    {
        get => _currentPhase;
        set
        {
            if (SetProperty(ref _currentPhase, value))
            {
                OnPropertyChanged(nameof(CanJoin));
                OnPropertyChanged(nameof(CanDeal));
                OnPropertyChanged(nameof(CanContinue));
                OnPropertyChanged(nameof(ShowCards));
                OnPropertyChanged(nameof(ShowSelectionCards));
                UpdateSelectionCanPick();
            }
        }
    }

    public bool IsJoined => Status == SeatStatus.Joined;
    public bool IsEmpty => Status == SeatStatus.Empty;

    public bool CanJoin => IsEmpty
        && (CurrentPhase == GamePhase.Attract || CurrentPhase == GamePhase.SeatJoin);

    public bool CanDeal => IsJoined && CurrentPhase == GamePhase.SeatJoin;

    public bool CanContinue => IsJoined && CurrentPhase == GamePhase.RoundEnd;

    public bool ShowCards => IsJoined
        && CurrentPhase != GamePhase.Attract && CurrentPhase != GamePhase.SeatJoin;

    public bool ShowSelectionCards => IsJoined && CurrentPhase == GamePhase.Picking;

    public ICommand JoinCommand { get; }
    public ICommand DealCommand { get; }
    public ICommand ContinueCommand { get; }

    public SeatViewModel(int seatId,
        Action<int> joinAction,
        Action<int> dealAction,
        Action<int> continueAction,
        Action<int, int> pickCardAction)
    {
        SeatId = seatId;
        _joinAction = joinAction;
        _dealAction = dealAction;
        _continueAction = continueAction;
        _pickCardAction = pickCardAction;

        JoinCommand = new RelayCommand(() => _joinAction(SeatId));
        DealCommand = new RelayCommand(() => _dealAction(SeatId));
        ContinueCommand = new RelayCommand(() => _continueAction(SeatId));

        // 5 selection card slots (clickable, picks card into hand)
        for (int i = 0; i < 5; i++)
        {
            var card = new CardViewModel(i)
            {
                OnToggleHold = idx => _pickCardAction(SeatId, idx)
            };
            SelectionCards.Add(card);
        }

        // 5 hand card slots (display only)
        for (int i = 0; i < 5; i++)
        {
            Cards.Add(new CardViewModel(i));
        }
    }

    public void UpdateFromSeat(Seat seat)
    {
        Status = seat.Status;
        TotalScore = seat.TotalScore;
        RoundPoints = seat.RoundPoints;
        HandName = seat.HandName;

        // Update selection cards
        for (int i = 0; i < 5; i++)
        {
            SelectionCards[i].Card = i < seat.SelectionCards.Length ? seat.SelectionCards[i] : null;
        }

        // Update hand cards
        for (int i = 0; i < 5; i++)
        {
            Cards[i].Card = i < seat.Hand.Length ? seat.Hand[i] : null;
            Cards[i].IsHeld = false;
        }

        UpdateSelectionCanPick();
    }

    private void UpdateSelectionCanPick()
    {
        bool canPick = IsJoined && CurrentPhase == GamePhase.Picking && IsActiveSeat && !IsTurnTransitioning;
        // Check if hand is already complete (5 cards)
        bool handComplete = Cards.Count(c => c.Card != null) >= 5;

        foreach (var card in SelectionCards)
            card.CanToggle = canPick && !handComplete;
    }
}
