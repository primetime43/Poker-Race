using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using RacePoker.Core.Engine;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;
using RacePoker.Core.Services;

namespace RacePoker.Wpf.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly GameEngine _engine;
    private readonly DispatcherTimer _timer;
    private DateTime _lastTick;

    private GamePhase _phase;
    private int _round;

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
            }
        }
    }

    public int Round
    {
        get => _round;
        private set => SetProperty(ref _round, value);
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

    public ICommand PressStartCommand { get; }
    public ICommand ExitGameCommand { get; }
    public ICommand PlayerJoinCommand { get; }

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

        for (int i = 0; i < _engine.Settings.MaxSeats; i++)
        {
            var seatVm = new SeatViewModel(i,
                joinAction: seatId => _engine.JoinSeat(seatId),
                leaveAction: seatId => _engine.LeaveSeat(seatId),
                addCreditAction: seatId => _engine.AddCredit(seatId),
                dealAction: seatId => _engine.PressDeal(seatId),
                continueAction: seatId => _engine.PressContinue(seatId),
                pickCardAction: (seatId, cardIdx) => _engine.PickCard(seatId, cardIdx));
            SeatViewModels.Add(seatVm);
        }

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
    /// Joins the seat and auto-deals to start the game.
    /// </summary>
    public void PlayerButtonPressed(int seatId)
    {
        if (seatId < 0 || seatId >= _engine.Settings.MaxSeats)
            return;

        var seat = _engine.Seats[seatId];
        if (seat.Status == SeatStatus.Empty)
        {
            _engine.JoinSeat(seatId);
        }

        // Auto-deal if we're in SeatJoin
        if (_engine.Phase == GamePhase.SeatJoin)
        {
            _engine.PressDeal(seatId);
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

    public void HandleKeyDown(Key key)
    {
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

            // Pick selection cards 1-5 for seat 0
            case Key.F1: _engine.PickCard(0, 0); break;
            case Key.F2: _engine.PickCard(0, 1); break;
            case Key.F3: _engine.PickCard(0, 2); break;
            case Key.F4: _engine.PickCard(0, 3); break;
            case Key.F5: _engine.PickCard(0, 4); break;

            // Continue for seat 1
            case Key.Enter: _engine.PressContinue(0); break;

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

        for (int i = 0; i < SeatViewModels.Count; i++)
        {
            SeatViewModels[i].CurrentPhase = Phase;
            SeatViewModels[i].UpdateFromSeat(_engine.Seats[i]);
        }
    }

    private void RefreshHighScores()
    {
        HighScores.Clear();
        foreach (var entry in _engine.HighScores.Entries)
            HighScores.Add(entry);
    }
}
