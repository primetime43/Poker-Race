using RacePoker.Core.Enums;
using RacePoker.Core.Models;
using RacePoker.Core.Services;

namespace RacePoker.Core.Engine;

public class GameEngine
{
    private readonly IRng _rng;
    private readonly Dictionary<int, Deck> _decks = new();
    private double _phaseTimer;
    private bool _turnTransitioning;

    public GamePhase Phase { get; private set; } = GamePhase.Attract;
    public int Round { get; private set; }
    public int ActiveSeatIndex { get; private set; }
    public bool IsTurnTransitioning => _turnTransitioning;

    /// <summary>
    /// How far through the current phase timer (0.0 = just started, 1.0 = done).
    /// </summary>
    public double PhaseTimerProgress
    {
        get
        {
            if (Phase != GamePhase.Picking || _turnTransitioning)
                return 1.0;
            if (Settings.PickingTimeoutSeconds <= 0)
                return 1.0;
            return Math.Clamp(1.0 - (_phaseTimer / Settings.PickingTimeoutSeconds), 0, 1);
        }
    }
    public Seat[] Seats { get; }
    public PointTable PointTable { get; set; }
    public GameSettings Settings { get; }
    public HighScoreService HighScores { get; }

    public event Action? StateChanged;

    public GameEngine(GameSettings? settings = null, PointTable? pointTable = null,
        IRng? rng = null, HighScoreService? highScores = null)
    {
        Settings = settings ?? new GameSettings();
        PointTable = pointTable ?? new PointTable();
        _rng = rng ?? new SystemRng();
        HighScores = highScores ?? new HighScoreService();

        Seats = new Seat[Settings.MaxSeats];
        for (int i = 0; i < Settings.MaxSeats; i++)
            Seats[i] = new Seat(i);

        HighScores.Load();
    }

    public bool JoinSeat(int seatId)
    {
        if (Phase != GamePhase.Attract && Phase != GamePhase.SeatJoin)
            return false;

        var seat = GetSeat(seatId);
        if (seat is null || seat.Status != SeatStatus.Empty)
            return false;

        seat.Status = SeatStatus.Joined;

        if (Phase == GamePhase.Attract)
            SetPhase(GamePhase.SeatJoin);

        OnStateChanged();
        return true;
    }

    public bool LeaveSeat(int seatId)
    {
        var seat = GetSeat(seatId);
        if (seat is null || seat.Status == SeatStatus.Empty)
            return false;

        seat.ResetFull();

        // If no seats left, return to attract
        if (!Seats.Any(s => s.Status != SeatStatus.Empty))
        {
            SetPhase(GamePhase.Attract);
            Round = 0;
        }

        OnStateChanged();
        return true;
    }

    /// <summary>
    /// Start dealing from SeatJoin phase. Deals 5 selection cards to each seat.
    /// </summary>
    public bool PressDeal(int seatId)
    {
        if (Phase != GamePhase.SeatJoin)
            return false;

        var seat = GetSeat(seatId);
        if (seat is null || seat.Status != SeatStatus.Joined)
            return false;

        StartDealing();
        return true;
    }

    /// <summary>
    /// Pick a card from the selection row during Picking phase.
    /// Only the active seat can pick cards (turn-based).
    /// </summary>
    public bool PickCard(int seatId, int selectionIndex)
    {
        if (Phase != GamePhase.Picking || _turnTransitioning)
            return false;
        if (seatId != ActiveSeatIndex)
            return false;

        var seat = GetSeat(seatId);
        if (seat is null || seat.Status != SeatStatus.Joined || seat.IsHandComplete)
            return false;
        if (selectionIndex < 0 || selectionIndex >= 5)
            return false;
        if (seat.SelectionCards.Length != 5)
            return false;

        // Take card from selection and add to hand
        var picked = seat.SelectionCards[selectionIndex];
        seat.Hand = seat.Hand.Append(picked).ToArray();

        // Replace selection card with new random card from deck
        var deck = _decks[seat.SeatId];
        var replacement = deck.Draw(1);
        seat.SelectionCards[selectionIndex] = replacement[0];

        // If this seat's hand is complete, evaluate and pause before next turn
        if (seat.IsHandComplete)
        {
            EvaluateSeat(seat);
            StartTurnTransition();
        }
        else
        {
            OnStateChanged();
        }

        return true;
    }

    public bool PressContinue(int seatId)
    {
        if (Phase != GamePhase.RoundEnd)
            return false;

        var seat = GetSeat(seatId);
        if (seat is null || seat.Status != SeatStatus.Joined)
            return false;

        AdvanceFromRoundEnd();
        OnStateChanged();
        return true;
    }

    public void Tick(TimeSpan dt)
    {
        _phaseTimer += dt.TotalSeconds;

        switch (Phase)
        {
            case GamePhase.Picking:
                if (_turnTransitioning)
                {
                    // Pausing to show completed hand before next player
                    if (_phaseTimer >= Settings.TurnDelaySeconds)
                    {
                        _turnTransitioning = false;
                        AdvanceActiveOrRace();
                    }
                }
                else if (_phaseTimer >= Settings.PickingTimeoutSeconds)
                {
                    // Timeout: auto-fill the active seat
                    var activeSeat = GetSeat(ActiveSeatIndex);
                    if (activeSeat != null && !activeSeat.IsHandComplete)
                    {
                        AutoFillHand(activeSeat);
                        EvaluateSeat(activeSeat);
                    }
                    StartTurnTransition();
                }
                break;

            case GamePhase.Racing:
                if (_phaseTimer >= Settings.RacingDurationSeconds)
                {
                    // Snap all seats to final score
                    foreach (var seat in AllJoinedSeats())
                        seat.TotalScore = seat.FinalScore;
                    SetPhase(GamePhase.RoundEnd);
                }
                else
                {
                    // Interpolate score with ease-out cubic
                    double t = Math.Clamp(_phaseTimer / Settings.RacingDurationSeconds, 0, 1);
                    double eased = 1.0 - Math.Pow(1.0 - t, 3);
                    foreach (var seat in AllJoinedSeats())
                    {
                        int delta = seat.FinalScore - seat.PreviousScore;
                        seat.TotalScore = seat.PreviousScore + (int)(delta * eased);
                    }
                    OnStateChanged();
                }
                break;

            case GamePhase.RoundEnd:
                var roundEndTimeout = Settings.AutoAdvanceEnabled
                    ? Settings.AutoAdvanceDelaySeconds
                    : Settings.RoundEndTimeoutSeconds;
                if (_phaseTimer >= roundEndTimeout)
                    AdvanceFromRoundEnd();
                break;

            case GamePhase.GameOver:
                if (_phaseTimer >= Settings.GameOverTimeoutSeconds)
                {
                    ResetGame();
                    SetPhase(GamePhase.Attract);
                }
                break;
        }
    }

    public void PressStartFromGameOver()
    {
        if (Phase == GamePhase.GameOver)
        {
            ResetGame();
            SetPhase(GamePhase.Attract);
        }
    }

    private void StartTurnTransition()
    {
        _turnTransitioning = true;
        _phaseTimer = 0;
        OnStateChanged();
    }

    private void StartDealing()
    {
        _turnTransitioning = false;
        Round++;
        _phaseTimer = 0;

        foreach (var seat in AllJoinedSeats())
        {
            seat.ResetForRound();

            if (!_decks.ContainsKey(seat.SeatId))
                _decks[seat.SeatId] = new Deck(_rng);
            else
                _decks[seat.SeatId].Shuffle();

            // Deal 5 face-up selection cards
            seat.SelectionCards = _decks[seat.SeatId].Draw(5);
            // Hand starts empty
            seat.Hand = Array.Empty<Card>();
        }

        ActiveSeatIndex = AllJoinedSeats().First().SeatId;
        SetPhase(GamePhase.Picking);
    }

    /// <summary>
    /// Advance to the next seat that hasn't completed their hand,
    /// or transition to Racing if all seats are done.
    /// </summary>
    private void AdvanceActiveOrRace()
    {
        var nextSeat = AllJoinedSeats().FirstOrDefault(s => !s.IsHandComplete);
        if (nextSeat != null)
        {
            ActiveSeatIndex = nextSeat.SeatId;
            _phaseTimer = 0; // reset per-player picking timer
            OnStateChanged();
        }
        else
        {
            // All players done — show first player's hand during racing
            ActiveSeatIndex = AllJoinedSeats().First().SeatId;
            SetPhase(GamePhase.Racing);
        }
    }

    private void AutoFillHand(Seat seat)
    {
        var deck = _decks[seat.SeatId];
        while (!seat.IsHandComplete)
        {
            // Pick the first selection card automatically
            var picked = seat.SelectionCards[0];
            seat.Hand = seat.Hand.Append(picked).ToArray();
            var replacement = deck.Draw(1);
            seat.SelectionCards[0] = replacement[0];
        }
    }

    private void EvaluateSeat(Seat seat)
    {
        var result = HandEvaluator.Evaluate(seat.Hand);
        seat.RoundPoints = PointTable.GetPoints(result);
        seat.PreviousScore = seat.TotalScore;
        seat.FinalScore = seat.TotalScore + seat.RoundPoints;
        // Don't add to TotalScore yet — Racing phase will animate it
        seat.HandName = FormatHandCategory(result.Category);
    }

    private void AdvanceFromRoundEnd()
    {
        // Check game over conditions
        bool maxRoundsReached = Round >= Settings.MaxRounds;
        bool finishCrossed = Seats.Any(s => s.Status == SeatStatus.Joined
            && s.TotalScore >= Settings.FinishDistance);

        if (maxRoundsReached || finishCrossed)
        {
            // Save high scores for all joined seats
            foreach (var seat in AllJoinedSeats())
            {
                HighScores.TryAdd(new HighScoreEntry
                {
                    Initials = $"P{seat.SeatId + 1}",
                    Score = seat.TotalScore,
                    Date = DateTime.Now
                });
            }

            SetPhase(GamePhase.GameOver);
        }
        else
        {
            StartDealing();
        }
    }

    private void ResetGame()
    {
        Round = 0;
        foreach (var seat in Seats)
            seat.ResetFull();
        _decks.Clear();
    }

    private void SetPhase(GamePhase phase)
    {
        Phase = phase;
        _phaseTimer = 0;
        OnStateChanged();
    }

    private Seat? GetSeat(int seatId)
    {
        if (seatId < 0 || seatId >= Seats.Length)
            return null;
        return Seats[seatId];
    }

    private IEnumerable<Seat> AllJoinedSeats()
        => Seats.Where(s => s.Status == SeatStatus.Joined);

    private void OnStateChanged() => StateChanged?.Invoke();

    private static string FormatHandCategory(HandCategory category) => category switch
    {
        HandCategory.HighCard => "High Card",
        HandCategory.OnePair => "Pair",
        HandCategory.TwoPair => "Two Pair",
        HandCategory.ThreeOfAKind => "Three of a Kind",
        HandCategory.Straight => "Straight",
        HandCategory.Flush => "Flush",
        HandCategory.FullHouse => "Full House",
        HandCategory.FourOfAKind => "Four of a Kind",
        HandCategory.StraightFlush => "Straight Flush",
        HandCategory.RoyalFlush => "Royal Flush",
        _ => category.ToString()
    };
}
