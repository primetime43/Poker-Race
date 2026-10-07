using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using RacePoker.Core.Engine;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;
using RacePoker.Core.Services;
using RacePoker.Wpf.Services;
using RacePoker.Wpf.Views;

namespace RacePoker.Wpf.ViewModels;

public record PointChartEntry(string Label, string Value);

public class MainViewModel : ViewModelBase
{
    private readonly GameEngine _engine;
    private readonly DispatcherTimer _timer;
    private readonly SettingsService _settingsService;
    private readonly SoundService _soundService;
    private DateTime _lastTick;

    private GamePhase _phase;
    private int _round;
    private GamePhase _previousPhase;
    private int _previousHandCount;
    private bool _previousTransitioning;
    private SeatViewModel? _activeSeat;
    private double _timerProgress;
    private string _winnerDisplay = "";
    private string _finalStandings = "";

    public GamePhase Phase
    {
        get => _phase;
        private set
        {
            if (SetProperty(ref _phase, value))
            {
                OnPropertyChanged(nameof(IsAttractVisible));
                OnPropertyChanged(nameof(IsGameVisible));
                OnPropertyChanged(nameof(IsInGame));
                OnPropertyChanged(nameof(IsShellMode));
                OnPropertyChanged(nameof(PhaseDisplay));
                OnPropertyChanged(nameof(RoundDisplay));
                OnPropertyChanged(nameof(ShowContinueButton));
            }
        }
    }

    public int Round
    {
        get => _round;
        private set
        {
            if (SetProperty(ref _round, value))
            {
                OnPropertyChanged(nameof(RoundDisplay));
                OnPropertyChanged(nameof(RoundNumberDisplay));
                OnPropertyChanged(nameof(RoundMultiplierDisplay));
            }
        }
    }

    /// <summary>
    /// The currently active seat's ViewModel. Card area binds to this.
    /// </summary>
    public SeatViewModel? ActiveSeat
    {
        get => _activeSeat;
        private set => SetProperty(ref _activeSeat, value);
    }

    public string PhaseDisplay => Phase switch
    {
        GamePhase.Attract => "ATTRACT",
        GamePhase.SeatJoin => "JOIN GAME",
        GamePhase.Picking => "PICK A CARD",
        GamePhase.Racing => "RACING",
        GamePhase.RoundEnd => "ROUND COMPLETE",
        GamePhase.GameOver => "GAME OVER",
        _ => Phase.ToString().ToUpper()
    };

    public string RoundDisplay => Round > 0 ? $"Round {Round}/{_engine.Settings.MaxRounds}" : "";
    public string RoundNumberDisplay => Round > 0 ? Round.ToString() : "";
    public string RoundMultiplierDisplay => Round > 0 ? $"x{1.0 + (Round - 1) * 0.15:F2}" : "";

    // Shell vs Game view navigation
    public bool IsInGame => Phase != GamePhase.Attract && Phase != GamePhase.SeatJoin;
    public bool IsShellMode => !IsInGame;

    // Legacy compat
    public bool IsAttractVisible => Phase == GamePhase.Attract;
    public bool IsGameVisible => Phase != GamePhase.Attract;

    public ObservableCollection<SeatViewModel> SeatViewModels { get; } = new();
    public ObservableCollection<HighScoreEntry> HighScores { get; } = new();
    public ObservableCollection<PointChartEntry> PointChartEntries { get; } = new();
    public ObservableCollection<InitialsEntryViewModel> InitialsEntries { get; } = new();

    public int FinishDistance => _engine.Settings.FinishDistance;
    public int MaxRounds => _engine.Settings.MaxRounds;
    public bool ShowContinueButton => Phase == GamePhase.RoundEnd && !_engine.Settings.AutoAdvanceEnabled;

    public bool UseSymbolIcon => _engine.Settings.RaceIconStyle == "Symbol";
    public bool UseImageIcon => _engine.Settings.RaceIconStyle != "Symbol";

    public bool UseHandResultMarquee => _engine.Settings.HandResultStyle == "Marquee";
    public bool UseHandResultLights => _engine.Settings.HandResultStyle == "Lights";

    public string RaceRulesDisplay => $"{FinishDistance:N0} points to finish · {MaxRounds} {(MaxRounds == 1 ? "round" : "rounds")}";
    public string HandProgressDisplay => $"{ActiveSeat?.Cards.Count(c => c.Card is not null) ?? 0} / 5 cards picked";
    public bool ShowSelectionRow => Phase == GamePhase.Picking && !_engine.IsTurnTransitioning;
    public bool HasInitialsEntries => InitialsEntries.Count > 0;
    public double TimerRemainingFraction => Phase == GamePhase.Picking && !_engine.IsTurnTransitioning
        ? TimerProgress : 0;
    public string CountdownDisplay => Phase == GamePhase.Picking && !_engine.IsTurnTransitioning
        ? (_engine.Settings.PickingTimeoutSeconds <= 0 ? "Take your time" : $"{Math.Ceiling(TimerRemainingFraction * _engine.Settings.PickingTimeoutSeconds):0}s to pick")
        : Phase == GamePhase.RoundEnd ? (_engine.Settings.AutoAdvanceEnabled ? "Next round shortly" : "Press Enter to continue") : "Hand complete";
    public string TurnInstruction => Phase switch
    {
        GamePhase.Picking when _engine.IsTurnTransitioning => "A good hand. A little more ground.",
        GamePhase.Picking => "Choose a card to build your hand",
        GamePhase.Racing => "All hands are in. Here come the standings.",
        GamePhase.RoundEnd => "Round complete",
        GamePhase.GameOver => "The race is decided",
        _ => "Ready to race"
    };
    public string HandResultDisplay => string.IsNullOrEmpty(ActiveSeat?.HandName)
        ? "Each pick is replaced with a fresh card." : $"{ActiveSeat.HandName} · +{ActiveSeat.RoundPoints:N0} points";
    public string SelectionHint => ShowSelectionRow
        ? "Click a card or press its number. A fresh card takes its place."
        : "Your hand is scored. Better hands move your horse farther.";
    public string LeaderDisplay
    {
        get
        {
            var joined = SeatViewModels.Where(s => s.IsJoined).ToArray();
            if (joined.Length == 0) return "Ready to race";
            int best = joined.Max(s => s.TotalScore);
            var leaders = joined.Where(s => s.TotalScore == best).ToArray();
            return best == 0 ? "The field is level. Make your first hand."
                : leaders.Length > 1 ? $"Tied for the lead · {best:N0} points"
                : $"{leaders[0].SeatLabel} leads · {best:N0} points";
        }
    }

    private void RefreshPlayFeedback()
    {
        foreach (var name in new[] { nameof(HandProgressDisplay), nameof(ShowSelectionRow), nameof(CountdownDisplay),
            nameof(TimerRemainingFraction), nameof(TurnInstruction), nameof(HandResultDisplay), nameof(SelectionHint), nameof(LeaderDisplay), nameof(HasInitialsEntries) })
            OnPropertyChanged(name);
    }

    public double CardSwayDegrees => _engine.Settings.CardSwayDegrees;
    public double LogoGlowBlur => _engine.Settings.LogoGlowBlur;

    /// <summary>Fraction of picking time remaining: 1.0 at the start, 0.0 at timeout.</summary>
    public double TimerProgress
    {
        get => _timerProgress;
        private set
        {
            if (SetProperty(ref _timerProgress, value))
            {
                OnPropertyChanged(nameof(TimerRemainingFraction));
                OnPropertyChanged(nameof(CountdownDisplay));
            }
        }
    }

    public string WinnerDisplay
    {
        get => _winnerDisplay;
        private set => SetProperty(ref _winnerDisplay, value);
    }

    public string FinalStandings
    {
        get => _finalStandings;
        private set => SetProperty(ref _finalStandings, value);
    }

    public ICommand PressStartCommand { get; }
    public ICommand ExitGameCommand { get; }
    public ICommand PlayerJoinCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenHowToPlayCommand { get; }

    public MainViewModel() : this(null, null) { }

    public MainViewModel(GameSettings? settings, string? dataDirectory)
    {
        var pointTablePath = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Config", "pointTable.json");
        var pointTable = PointTableService.Load(pointTablePath);

        _settingsService = new SettingsService(dataDirectory is null ? null : System.IO.Path.Combine(dataDirectory, "settings.json"));
        settings ??= _settingsService.Load();
        var highScores = new HighScoreService(dataDirectory is null ? null : System.IO.Path.Combine(dataDirectory, "highscores.json"));
        _engine = new GameEngine(settings: settings, pointTable: pointTable, highScores: highScores);
        _engine.StateChanged += OnEngineStateChanged;
        _soundService = new SoundService(settings, dataDirectory is null ? null : System.IO.Path.Combine(dataDirectory, "Sounds"));

        PressStartCommand = new RelayCommand(() =>
        {
            if (Phase == GamePhase.GameOver)
            {
                SaveInitials();
                _engine.PressStartFromGameOver();
            }
        });

        ExitGameCommand = new RelayCommand(ExitGame);

        PlayerJoinCommand = new RelayCommand(
            p => PlayerButtonPressed(int.Parse((string)p!)),
            p => Phase == GamePhase.Attract || Phase == GamePhase.SeatJoin);

        OpenSettingsCommand = new RelayCommand(OpenSettings);
        OpenHowToPlayCommand = new RelayCommand(OpenHowToPlay);

        for (int i = 0; i < _engine.Settings.MaxSeats; i++)
        {
            var seatVm = new SeatViewModel(i,
                joinAction: seatId => _engine.JoinSeat(seatId),
                dealAction: seatId => _engine.PressDeal(seatId),
                continueAction: seatId => _engine.PressContinue(seatId),
                pickCardAction: (seatId, cardIdx) => _engine.PickCard(seatId, cardIdx));
            SeatViewModels.Add(seatVm);
        }

        ActiveSeat = SeatViewModels[0];

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60fps
        };
        _timer.Tick += OnTimerTick;
        _lastTick = DateTime.UtcNow;
        _timer.Start();

        RefreshHighScores();
        BuildPointChart();
        SyncFromEngine();
    }

    /// <summary>
    /// Called when a player button (1-4) is pressed on the shell.
    /// The button index represents the number of players: 0 = 1 player, 3 = 4 players.
    /// Joins seats 0 through buttonIndex and auto-deals.
    /// </summary>
    public void PlayerButtonPressed(int buttonIndex)
    {
        if (!IsShellMode || buttonIndex < 0 || buttonIndex >= _engine.Settings.MaxSeats)
            return;

        // Join seats 0 through buttonIndex
        for (int i = 0; i <= buttonIndex; i++)
        {
            if (_engine.Seats[i].Status == SeatStatus.Empty)
                _engine.JoinSeat(i);
        }

        // Auto-deal if we're in SeatJoin
        if (_engine.Phase == GamePhase.SeatJoin)
        {
            _engine.PressDeal(0);
        }
    }

    /// <summary>
    /// X button: exit game, leave all seats, return to attract shell.
    /// </summary>
    public void ExitGame()
    {
        if (_engine.Phase == GamePhase.GameOver)
            SaveInitials();

        for (int i = 0; i < _engine.Settings.MaxSeats; i++)
            _engine.LeaveSeat(i);

        if (_engine.Phase == GamePhase.GameOver)
            _engine.PressStartFromGameOver();
    }

    public void OpenHowToPlay()
    {
        var win = new HowToPlayWindow
        {
            DataContext = this,
            Owner = Application.Current.MainWindow
        };
        ShowPausedDialog(win);
    }

    public void OpenSettings()
    {
        var vm = new SettingsViewModel(_engine.Settings);
        var win = new SettingsWindow
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };
        ShowPausedDialog(win);
        _settingsService.Save(_engine.Settings);

        OnPropertyChanged(nameof(UseSymbolIcon));
        OnPropertyChanged(nameof(UseImageIcon));
        OnPropertyChanged(nameof(UseHandResultMarquee));
        OnPropertyChanged(nameof(UseHandResultLights));
        OnPropertyChanged(nameof(CardSwayDegrees));
        OnPropertyChanged(nameof(LogoGlowBlur));
        OnPropertyChanged(nameof(FinishDistance));
        OnPropertyChanged(nameof(MaxRounds));
        OnPropertyChanged(nameof(RaceRulesDisplay));
        OnPropertyChanged(nameof(RoundDisplay));
        OnPropertyChanged(nameof(ShowContinueButton));
        RefreshPlayFeedback();
    }

    private void ShowPausedDialog(Window window)
    {
        _timer.Stop();
        try { window.ShowDialog(); }
        finally { _lastTick = DateTime.UtcNow; _timer.Start(); }
    }

    public void HandleKeyDown(Key key)
    {
        var active = _engine.ActiveSeatIndex;

        switch (key)
        {
            // Number keys: shell = join+start, picking = pick card, otherwise = join/deal
            case Key.D1:
            case Key.NumPad1:
                if (IsShellMode) PlayerButtonPressed(0);
                else if (Phase == GamePhase.Picking) _engine.PickCard(active, 0);
                else ToggleJoinOrDeal(0);
                break;
            case Key.D2:
            case Key.NumPad2:
                if (IsShellMode) PlayerButtonPressed(1);
                else if (Phase == GamePhase.Picking) _engine.PickCard(active, 1);
                else ToggleJoinOrDeal(1);
                break;
            case Key.D3:
            case Key.NumPad3:
                if (IsShellMode) PlayerButtonPressed(2);
                else if (Phase == GamePhase.Picking) _engine.PickCard(active, 2);
                else ToggleJoinOrDeal(2);
                break;
            case Key.D4:
            case Key.NumPad4:
                if (IsShellMode) PlayerButtonPressed(3);
                else if (Phase == GamePhase.Picking) _engine.PickCard(active, 3);
                else ToggleJoinOrDeal(3);
                break;
            case Key.D5:
            case Key.NumPad5:
                if (Phase == GamePhase.Picking) _engine.PickCard(active, 4);
                break;

            // Deal for seat 1
            case Key.Space:
                if (IsShellMode) PlayerButtonPressed(0);
                else _engine.PressDeal(0);
                break;

            // Pick selection cards 1-5 for the ACTIVE seat
            case Key.F1: _engine.PickCard(active, 0); break;
            case Key.F2: _engine.PickCard(active, 1); break;
            case Key.F3: _engine.PickCard(active, 2); break;
            case Key.F4: _engine.PickCard(active, 3); break;
            case Key.F5: _engine.PickCard(active, 4); break;

            // Continue (advances all seats)
            case Key.Enter:
                if (IsShellMode) PlayerButtonPressed(0);
                else if (Phase == GamePhase.GameOver) ExitGame();
                else _engine.PressContinue(active);
                break;

            // Exit game / return to shell
            case Key.Escape:
                if (IsInGame)
                    ExitGame();
                break;
        }
    }

    private void ToggleJoinOrDeal(int seatId)
    {
        var seat = _engine.Seats[seatId];
        if (seat.Status == SeatStatus.Empty)
            _engine.JoinSeat(seatId);
        else if (Phase == GamePhase.SeatJoin)
            _engine.PressDeal(seatId);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        var dt = now - _lastTick;
        _lastTick = now;
        _engine.Tick(dt);
        TimerProgress = _engine.PhaseTimerProgress;
    }

    private void OnEngineStateChanged()
    {
        if (_previousPhase == GamePhase.GameOver && _engine.Phase == GamePhase.Attract)
            SaveInitials();
        SyncFromEngine();

        if (_engine.Phase == GamePhase.GameOver || _engine.Phase == GamePhase.Attract)
            RefreshHighScores();
    }

    private void SyncFromEngine()
    {
        var newPhase = _engine.Phase;
        bool enteredGameOver = newPhase == GamePhase.GameOver && _previousPhase != GamePhase.GameOver;
        if (_engine.IsTurnTransitioning && !_previousTransitioning)
            _soundService.Play("hand_complete");
        _previousTransitioning = _engine.IsTurnTransitioning;

        // Detect hand card count increase for card_pick sound
        var activeSeat = _engine.Seats[_engine.ActiveSeatIndex];
        int currentHandCount = activeSeat.Hand.Length;
        if (currentHandCount > _previousHandCount && currentHandCount < 5 && newPhase == GamePhase.Picking)
            _soundService.Play("card_pick");
        _previousHandCount = currentHandCount;

        // Detect phase transitions for sounds
        if (newPhase != _previousPhase)
        {
            if (newPhase == GamePhase.Racing)
                _soundService.Play("racing");
            if (newPhase == GamePhase.RoundEnd)
                _soundService.Play("round_end");
            if (newPhase == GamePhase.GameOver)
            {
                var winner = _engine.Seats.Any(s => s.Status == RacePoker.Core.Enums.SeatStatus.Joined);
                _soundService.Play(winner ? "game_win" : "game_over");
            }
            _previousPhase = newPhase;
        }

        Phase = _engine.Phase;
        Round = _engine.Round;

        var activeIdx = _engine.ActiveSeatIndex;
        var transitionProgress = _engine.TurnTransitionProgress;

        for (int i = 0; i < SeatViewModels.Count; i++)
        {
            SeatViewModels[i].CurrentPhase = Phase;
            SeatViewModels[i].IsActiveSeat = (i == activeIdx);
            SeatViewModels[i].IsTurnTransitioning = _engine.IsTurnTransitioning;
            SeatViewModels[i].UpdateFromSeat(_engine.Seats[i]);

            // Animate the active seat's horse during turn transition; others stay put
            var seat = _engine.Seats[i];
            if (_engine.IsTurnTransitioning && i == activeIdx)
            {
                int delta = seat.FinalScore - seat.PreviousScore;
                SeatViewModels[i].TrackScore = seat.PreviousScore + (int)(delta * transitionProgress);
            }
            else
            {
                SeatViewModels[i].TrackScore = seat.TotalScore;
            }
        }

        if (activeIdx >= 0 && activeIdx < SeatViewModels.Count)
            ActiveSeat = SeatViewModels[activeIdx];

        if (enteredGameOver)
            ComputeWinner();
        TimerProgress = _engine.PhaseTimerProgress;
        RefreshPlayFeedback();
    }

    private void ComputeWinner()
    {
        var joined = _engine.Seats
            .Where(s => s.Status == SeatStatus.Joined)
            .OrderByDescending(s => s.TotalScore)
            .ToList();

        if (joined.Count == 0)
        {
            WinnerDisplay = "";
            FinalStandings = "";
            return;
        }

        var topScore = joined[0].TotalScore;
        var winners = joined.Where(s => s.TotalScore == topScore).ToList();

        WinnerDisplay = winners.Count > 1
            ? "IT'S A TIE!"
            : $"PLAYER {winners[0].SeatId + 1} WINS!";

        var lines = new System.Text.StringBuilder();
        for (int i = 0; i < joined.Count; i++)
        {
            var s = joined[i];
            int rank = joined.FindIndex(other => other.TotalScore == s.TotalScore) + 1;
            lines.AppendLine($"#{rank}  Player {s.SeatId + 1}  —  {s.TotalScore:N0} pts");
        }
        FinalStandings = lines.ToString().TrimEnd();

        // Populate initials entries for qualifying players
        InitialsEntries.Clear();
        foreach (var s in joined)
        {
            if (_engine.HighScores.QualifiesForHighScore(s.TotalScore))
                InitialsEntries.Add(new InitialsEntryViewModel(s.SeatId, s.TotalScore));
        }
    }

    private void SaveInitials()
    {
        foreach (var entry in InitialsEntries)
        {
            var initials = string.IsNullOrWhiteSpace(entry.Initials)
                ? $"P{entry.SeatId + 1}"
                : entry.Initials;
            _engine.HighScores.TryAdd(new HighScoreEntry
            {
                Initials = initials,
                Score = entry.Score,
                Date = DateTime.Now
            });
        }
        InitialsEntries.Clear();
        RefreshHighScores();
        OnPropertyChanged(nameof(HasInitialsEntries));
    }

    public void Stop()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _engine.StateChanged -= OnEngineStateChanged;
        if (Phase == GamePhase.GameOver) SaveInitials();
    }

    private void RefreshHighScores()
    {
        HighScores.Clear();
        foreach (var entry in _engine.HighScores.Entries)
            HighScores.Add(entry);
    }

    private void BuildPointChart()
    {
        var pts = _engine.PointTable.Points;

        string Fmt(HandCategory cat) =>
            pts.TryGetValue(cat, out var v) && v > 0 ? v.ToString("N0") : "Face Value";

        // Row 1: High Card, 2 Pair, Straight, Full House, Str. Flush
        // Row 2: Any Pair, 3 of a Kind, Flush, 4 of a Kind, Royal Flush
        var layout = new (string Label, HandCategory Cat)[]
        {
            ("High Card",    HandCategory.HighCard),
            ("2 Pair",       HandCategory.TwoPair),
            ("Straight",     HandCategory.Straight),
            ("Full House",   HandCategory.FullHouse),
            ("Str. Flush",   HandCategory.StraightFlush),
            ("Any Pair",     HandCategory.OnePair),
            ("3 Of A Kind",  HandCategory.ThreeOfAKind),
            ("Flush",        HandCategory.Flush),
            ("4 Of A Kind",  HandCategory.FourOfAKind),
            ("Royal Flush",  HandCategory.RoyalFlush),
        };

        PointChartEntries.Clear();
        foreach (var (label, cat) in layout)
            PointChartEntries.Add(new PointChartEntry(label, Fmt(cat)));
    }
}
