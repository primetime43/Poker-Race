using RacePoker.Core.Enums;
using RacePoker.Core.Models;
using RacePoker.Core.Services;

namespace RacePoker.Core.Engine;

public class GameEngine
{
    private readonly IRng _rng;
    private readonly Dictionary<int, Deck> _decks = new();
    private double _phaseTimer;

    public GamePhase Phase { get; private set; } = GamePhase.Attract;
    public int Round { get; private set; }
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

        if (seat.Credits < Settings.CreditCostPerRound)
            return false;

        seat.Status = SeatStatus.Joined;
        seat.Credits -= Settings.CreditCostPerRound;

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

    public bool AddCredit(int seatId, int amount = 1)
    {
        var seat = GetSeat(seatId);
        if (seat is null || amount <= 0)
            return false;

        seat.Credits += amount;
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
    /// The picked card is added to the player's hand, and a new random card
    /// replaces it in the selection row.
    /// </summary>
    public bool PickCard(int seatId, int selectionIndex)
    {
        if (Phase != GamePhase.Picking)
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

        // If this seat's hand is complete, auto-evaluate
        if (seat.IsHandComplete)
        {
            EvaluateSeat(seat);
        }

        // If ALL joined seats have complete hands, advance to Racing
        if (AllJoinedSeats().All(s => s.IsHandComplete))
        {
            SetPhase(GamePhase.Racing);
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

        seat.IsReadyToContinue = true;

        if (AllJoinedSeats().All(s => s.IsReadyToContinue))
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
                if (_phaseTimer >= Settings.PickingTimeoutSeconds)
                {
                    // Auto-fill remaining hands with random picks
                    foreach (var seat in AllJoinedSeats().Where(s => !s.IsHandComplete))
                    {
                        AutoFillHand(seat);
                        EvaluateSeat(seat);
                    }
                    SetPhase(GamePhase.Racing);
                }
                break;

            case GamePhase.Racing:
                if (_phaseTimer >= Settings.RacingDurationSeconds)
                    SetPhase(GamePhase.RoundEnd);
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

    private void StartDealing()
    {
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

        SetPhase(GamePhase.Picking);
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
        seat.TotalScore += seat.RoundPoints;
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
            // Deduct credits for next round
            foreach (var seat in AllJoinedSeats())
            {
                if (seat.Credits < Settings.CreditCostPerRound)
                {
                    seat.Status = SeatStatus.Folded;
                }
                else
                {
                    seat.Credits -= Settings.CreditCostPerRound;
                }
            }

            // If no seats left with credits, game over
            if (!Seats.Any(s => s.Status == SeatStatus.Joined))
            {
                SetPhase(GamePhase.GameOver);
                return;
            }

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
