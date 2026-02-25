using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using RacePoker.Core.Engine;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;
using RacePoker.Core.Services;
using RacePoker.Wpf.Views;

namespace RacePoker.Wpf.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly GameEngine _engine;
    private readonly DispatcherTimer _timer;
    private DateTime _lastTick;

    private GamePhase _phase;
    private int _round;
    private SeatViewModel? _activeSeat;

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
        private set => SetProperty(ref _round, value);
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
        GamePhase.Dealing => "DEALING",
        GamePhase.Picking => "PICK A CARD",
        GamePhase.Scoring => "SCORING",
        GamePhase.Racing => "RACING",
        GamePhase.RoundEnd => "ROUND COMPLETE",
        GamePhase.GameOver => "GAME OVER",
        _ => Phase.ToString().ToUpper()
    };

    public string RoundDisplay => Round > 0 ? $"Round {Round}/{_engine.Settings.MaxRounds}" : "";

    // Shell vs Game view navigation
    public bool IsInGame => Phase != GamePhase.Attract && Phase != GamePhase.SeatJoin;
    public bool IsShellMode => !IsInGame;

    // Legacy compat
    public bool IsAttractVisible => Phase == GamePhase.Attract;
    public bool IsGameVisible => Phase != GamePhase.Attract;

    public ObservableCollection<SeatViewModel> SeatViewModels { get; } = new();
    public ObservableCollection<HighScoreEntry> HighScores { get; } = new();

    public int FinishDistance => _engine.Settings.FinishDistance;
    public int MaxRounds => _engine.Settings.MaxRounds;
    public bool ShowContinueButton => Phase == GamePhase.RoundEnd && !_engine.Settings.AutoAdvanceEnabled;

    public ICommand PressStartCommand { get; }
    public ICommand ExitGameCommand { get; }
    public ICommand PlayerJoinCommand { get; }
    public ICommand OpenSettingsCommand { get; }

    public MainViewModel()
    {
        var pointTablePath = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Config", "pointTable.json");
        var pointTable = PointTableService.Load(pointTablePath);

        _engine = new GameEngine(pointTable: pointTable);
        _engine.StateChanged += OnEngineStateChanged;

        PressStartCommand = new RelayCommand(() =>
        {
            if (Phase == GamePhase.GameOver)
                _engine.PressStartFromGameOver();
        });

        ExitGameCommand = new RelayCommand(ExitGame);

        PlayerJoinCommand = new RelayCommand(
            p => PlayerButtonPressed(int.Parse((string)p!)),
            p => Phase == GamePhase.Attract || Phase == GamePhase.SeatJoin);

        OpenSettingsCommand = new RelayCommand(OpenSettings);

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
        SyncFromEngine();
    }

    /// <summary>
    /// Called when a player button (1-4) is pressed on the Megatouch shell.
    /// The button index represents the number of players: 0 = 1 player, 3 = 4 players.
    /// Joins seats 0 through buttonIndex and auto-deals.
    /// </summary>
    public void PlayerButtonPressed(int buttonIndex)
    {
        if (buttonIndex < 0 || buttonIndex >= _engine.Settings.MaxSeats)
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
        for (int i = 0; i < _engine.Settings.MaxSeats; i++)
            _engine.LeaveSeat(i);

        if (_engine.Phase == GamePhase.GameOver)
            _engine.PressStartFromGameOver();
    }

    public void OpenSettings()
    {
        var vm = new SettingsViewModel(_engine.Settings);
        var win = new SettingsWindow
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };
        win.ShowDialog();
    }

    public void HandleKeyDown(Key key)
    {
        var active = _engine.ActiveSeatIndex;

        switch (key)
        {
            // Player buttons 1-4 (in shell: join+start, in game: various)
            case Key.D1:
                if (IsShellMode) PlayerButtonPressed(0);
                else ToggleJoinOrDeal(0);
                break;
            case Key.D2:
                if (IsShellMode) PlayerButtonPressed(1);
                else ToggleJoinOrDeal(1);
                break;
            case Key.D3:
                if (IsShellMode) PlayerButtonPressed(2);
                else ToggleJoinOrDeal(2);
                break;
            case Key.D4:
                if (IsShellMode) PlayerButtonPressed(3);
                else ToggleJoinOrDeal(3);
                break;

            // Deal for seat 1
            case Key.Space: _engine.PressDeal(0); break;

            // Pick selection cards 1-5 for the ACTIVE seat
            case Key.F1: _engine.PickCard(active, 0); break;
            case Key.F2: _engine.PickCard(active, 1); break;
            case Key.F3: _engine.PickCard(active, 2); break;
            case Key.F4: _engine.PickCard(active, 3); break;
            case Key.F5: _engine.PickCard(active, 4); break;

            // Continue (advances all seats)
            case Key.Enter: _engine.PressContinue(active); break;

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
    }

    private void OnEngineStateChanged()
    {
        SyncFromEngine();

        if (_engine.Phase == GamePhase.GameOver || _engine.Phase == GamePhase.Attract)
            RefreshHighScores();
    }

    private void SyncFromEngine()
    {
        Phase = _engine.Phase;
        Round = _engine.Round;

        var activeIdx = _engine.ActiveSeatIndex;

        for (int i = 0; i < SeatViewModels.Count; i++)
        {
            SeatViewModels[i].CurrentPhase = Phase;
            SeatViewModels[i].IsActiveSeat = (i == activeIdx);
            SeatViewModels[i].UpdateFromSeat(_engine.Seats[i]);
        }

        if (activeIdx >= 0 && activeIdx < SeatViewModels.Count)
            ActiveSeat = SeatViewModels[activeIdx];
    }

    private void RefreshHighScores()
    {
        HighScores.Clear();
        foreach (var entry in _engine.HighScores.Entries)
            HighScores.Add(entry);
    }
}
