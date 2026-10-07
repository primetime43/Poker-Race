using RacePoker.Core.Engine;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;
using RacePoker.Core.Services;

namespace RacePoker.Tests;

public class GameEngineTests
{
    private static GameEngine CreateEngine(GameSettings? settings = null)
    {
        var s = settings ?? new GameSettings();
        var tempFile = Path.GetTempFileName();
        return new GameEngine(s, rng: new SeededRng(42), highScores: new HighScoreService(tempFile));
    }

    /// <summary>
    /// Helper: complete a hand and tick past the turn transition delay.
    /// </summary>
    private static void PickAllCardsAndWait(GameEngine engine, int seatId)
    {
        PickAllCards(engine, seatId);
        if (engine.IsTurnTransitioning)
            engine.Tick(TimeSpan.FromSeconds(engine.Settings.TurnDelaySeconds + 0.1));
    }

    /// <summary>
    /// Helper: pick all 5 cards from selection slot 0 to fill the hand.
    /// </summary>
    private static void PickAllCards(GameEngine engine, int seatId)
    {
        for (int i = 0; i < 5; i++)
            engine.PickCard(seatId, 0);
    }

    [Fact]
    public void InitialPhaseIsAttract()
    {
        var engine = CreateEngine();
        Assert.Equal(GamePhase.Attract, engine.Phase);
    }

    [Fact]
    public void JoinSeat_MovesToSeatJoin()
    {
        var engine = CreateEngine();
        engine.JoinSeat(0);
        Assert.Equal(GamePhase.SeatJoin, engine.Phase);
        Assert.Equal(SeatStatus.Joined, engine.Seats[0].Status);
    }

    [Fact]
    public void PressDeal_StartsPickingFromSeatJoin()
    {
        var engine = CreateEngine();
        engine.JoinSeat(0);
        engine.PressDeal(0);
        Assert.Equal(GamePhase.Picking, engine.Phase);
        Assert.Equal(5, engine.Seats[0].SelectionCards.Length);
        Assert.Empty(engine.Seats[0].Hand);
        Assert.Equal(1, engine.Round);
    }

    [Fact]
    public void PickCard_AddsToHandAndReplacesSelection()
    {
        var engine = CreateEngine();
        engine.JoinSeat(0);
        engine.PressDeal(0);

        var originalSelection = engine.Seats[0].SelectionCards[0];
        var result = engine.PickCard(0, 0);

        Assert.True(result);
        Assert.Single(engine.Seats[0].Hand);
        Assert.Equal(originalSelection, engine.Seats[0].Hand[0]);
        // Selection card at index 0 should be replaced with a new card
        Assert.NotEqual(originalSelection, engine.Seats[0].SelectionCards[0]);
    }

    [Fact]
    public void PickCard_FillsHandSequentially()
    {
        var engine = CreateEngine();
        engine.JoinSeat(0);
        engine.PressDeal(0);

        engine.PickCard(0, 2); // pick from slot 2
        Assert.Single(engine.Seats[0].Hand);

        engine.PickCard(0, 0); // pick from slot 0
        Assert.Equal(2, engine.Seats[0].Hand.Length);

        engine.PickCard(0, 4); // pick from slot 4
        Assert.Equal(3, engine.Seats[0].Hand.Length);
    }

    [Fact]
    public void PickCard_CompletesHandAndScores()
    {
        var engine = CreateEngine();
        engine.JoinSeat(0);
        engine.PressDeal(0);

        // Pick 5 cards to complete hand (wait for turn transition)
        PickAllCardsAndWait(engine, 0);

        // Single player: should advance to Racing after transition
        Assert.Equal(GamePhase.Racing, engine.Phase);
        Assert.True(engine.Seats[0].TotalScore >= 0);
        Assert.NotEmpty(engine.Seats[0].HandName);
        Assert.True(engine.Seats[0].IsHandComplete);
    }

    [Fact]
    public void PickCard_FailsWhenHandComplete()
    {
        var engine = CreateEngine();
        engine.JoinSeat(0);
        engine.PressDeal(0);
        PickAllCardsAndWait(engine, 0);

        // Hand is complete and phase has changed to Racing
        var result = engine.PickCard(0, 0);
        Assert.False(result);
    }

    [Fact]
    public void PickCard_FailsOutsidePickingPhase()
    {
        var engine = CreateEngine();
        Assert.False(engine.PickCard(0, 0));
    }

    [Fact]
    public void PickingTimeout_AutoFillsHand()
    {
        var settings = new GameSettings { PickingTimeoutSeconds = 1.0 };
        var engine = CreateEngine(settings);
        engine.JoinSeat(0);
        engine.PressDeal(0);

        // Pick only 2 cards
        engine.PickCard(0, 0);
        engine.PickCard(0, 1);
        Assert.Equal(2, engine.Seats[0].Hand.Length);
        Assert.Equal(GamePhase.Picking, engine.Phase);

        // Tick past the timeout — enters turn transition
        engine.Tick(TimeSpan.FromSeconds(2));
        Assert.Equal(5, engine.Seats[0].Hand.Length);
        Assert.NotEmpty(engine.Seats[0].HandName);

        // Tick past turn delay — advances to Racing
        engine.Tick(TimeSpan.FromSeconds(settings.TurnDelaySeconds + 0.1));
        Assert.Equal(GamePhase.Racing, engine.Phase);
    }

    [Fact]
    public void RacingTimeout_MovesToRoundEnd()
    {
        var settings = new GameSettings { RacingDurationSeconds = 0.5 };
        var engine = CreateEngine(settings);
        engine.JoinSeat(0);
        engine.PressDeal(0);
        PickAllCardsAndWait(engine, 0); // complete hand → Racing

        Assert.Equal(GamePhase.Racing, engine.Phase);
        engine.Tick(TimeSpan.FromSeconds(1));
        Assert.Equal(GamePhase.RoundEnd, engine.Phase);
    }

    [Fact]
    public void PressContinue_AdvancesToNextRound()
    {
        var settings = new GameSettings { RacingDurationSeconds = 0.1 };
        var engine = CreateEngine(settings);
        engine.JoinSeat(0);
        engine.PressDeal(0);
        PickAllCardsAndWait(engine, 0);
        engine.Tick(TimeSpan.FromSeconds(1)); // racing -> round end

        Assert.Equal(GamePhase.RoundEnd, engine.Phase);
        engine.PressContinue(0);

        Assert.Equal(GamePhase.Picking, engine.Phase);
        Assert.Equal(2, engine.Round);
        // New round: selection cards dealt, hand empty
        Assert.Equal(5, engine.Seats[0].SelectionCards.Length);
        Assert.Empty(engine.Seats[0].Hand);
    }

    [Fact]
    public void GameOver_AfterMaxRounds()
    {
        var settings = new GameSettings
        {
            MaxRounds = 2,
            RacingDurationSeconds = 0.01,
            RoundEndTimeoutSeconds = 100
        };
        var engine = CreateEngine(settings);
        engine.JoinSeat(0);

        // Round 1
        engine.PressDeal(0);
        PickAllCardsAndWait(engine, 0);
        engine.Tick(TimeSpan.FromSeconds(1)); // racing -> round end
        engine.PressContinue(0); // next round

        // Round 2
        PickAllCardsAndWait(engine, 0);
        engine.Tick(TimeSpan.FromSeconds(1)); // racing -> round end

        // At round end with round == maxRounds, pressing continue triggers game over
        engine.PressContinue(0);
        Assert.Equal(GamePhase.GameOver, engine.Phase);
    }

    [Fact]
    public void LeaveSeat_ReturnsToAttractWhenNoSeatsLeft()
    {
        var engine = CreateEngine();
        engine.JoinSeat(0);
        engine.LeaveSeat(0);
        Assert.Equal(GamePhase.Attract, engine.Phase);
        Assert.Equal(SeatStatus.Empty, engine.Seats[0].Status);
    }

    [Fact]
    public void MultipleSeatJoin()
    {
        var engine = CreateEngine();
        engine.JoinSeat(0);
        engine.JoinSeat(1);
        Assert.Equal(SeatStatus.Joined, engine.Seats[0].Status);
        Assert.Equal(SeatStatus.Joined, engine.Seats[1].Status);
    }

    [Fact]
    public void MultipleSeats_TurnBasedPicking()
    {
        var engine = CreateEngine();
        engine.JoinSeat(0);
        engine.JoinSeat(1);
        engine.PressDeal(0); // start dealing for both

        Assert.Equal(GamePhase.Picking, engine.Phase);
        Assert.Equal(0, engine.ActiveSeatIndex);

        // Seat 0 picks all 5 — enters turn transition
        PickAllCards(engine, 0);
        Assert.True(engine.IsTurnTransitioning);
        Assert.True(engine.Seats[0].IsHandComplete);

        // Tick past turn delay — turn passes to seat 1
        engine.Tick(TimeSpan.FromSeconds(engine.Settings.TurnDelaySeconds + 0.1));
        Assert.Equal(GamePhase.Picking, engine.Phase);
        Assert.Equal(1, engine.ActiveSeatIndex);
        Assert.False(engine.Seats[1].IsHandComplete);

        // Seat 1 picks all 5 — transition then Racing
        PickAllCardsAndWait(engine, 1);
        Assert.Equal(GamePhase.Racing, engine.Phase);
        Assert.True(engine.Seats[1].IsHandComplete);
    }

    [Fact]
    public void AutoAdvance_AdvancesAfterDelaySeconds()
    {
        var settings = new GameSettings
        {
            RacingDurationSeconds = 0.1,
            AutoAdvanceEnabled = true,
            AutoAdvanceDelaySeconds = 2.0
        };
        var engine = CreateEngine(settings);
        engine.JoinSeat(0);
        engine.PressDeal(0);
        PickAllCardsAndWait(engine, 0);
        engine.Tick(TimeSpan.FromSeconds(1)); // racing -> round end

        Assert.Equal(GamePhase.RoundEnd, engine.Phase);

        // Tick less than auto-advance delay — should stay in RoundEnd
        engine.Tick(TimeSpan.FromSeconds(1));
        Assert.Equal(GamePhase.RoundEnd, engine.Phase);

        // Tick past auto-advance delay — should advance
        engine.Tick(TimeSpan.FromSeconds(1.5));
        Assert.Equal(GamePhase.Picking, engine.Phase);
        Assert.Equal(2, engine.Round);
    }

    [Fact]
    public void AutoAdvance_ManualContinueStillWorks()
    {
        var settings = new GameSettings
        {
            RacingDurationSeconds = 0.1,
            AutoAdvanceEnabled = true,
            AutoAdvanceDelaySeconds = 10.0
        };
        var engine = CreateEngine(settings);
        engine.JoinSeat(0);
        engine.PressDeal(0);
        PickAllCardsAndWait(engine, 0);
        engine.Tick(TimeSpan.FromSeconds(1)); // racing -> round end

        Assert.Equal(GamePhase.RoundEnd, engine.Phase);

        // Manual continue before auto-advance fires
        engine.PressContinue(0);
        Assert.Equal(GamePhase.Picking, engine.Phase);
        Assert.Equal(2, engine.Round);
    }

    [Fact]
    public void AutoAdvance_DisabledFallsBackToRoundEndTimeout()
    {
        var settings = new GameSettings
        {
            RacingDurationSeconds = 0.1,
            AutoAdvanceEnabled = false,
            AutoAdvanceDelaySeconds = 1.0,
            RoundEndTimeoutSeconds = 5.0
        };
        var engine = CreateEngine(settings);
        engine.JoinSeat(0);
        engine.PressDeal(0);
        PickAllCardsAndWait(engine, 0);
        engine.Tick(TimeSpan.FromSeconds(1)); // racing -> round end

        Assert.Equal(GamePhase.RoundEnd, engine.Phase);

        // Tick past AutoAdvanceDelaySeconds but not RoundEndTimeoutSeconds
        engine.Tick(TimeSpan.FromSeconds(2));
        Assert.Equal(GamePhase.RoundEnd, engine.Phase); // should NOT advance

        // Tick past RoundEndTimeoutSeconds
        engine.Tick(TimeSpan.FromSeconds(4));
        Assert.Equal(GamePhase.Picking, engine.Phase);
        Assert.Equal(2, engine.Round);
    }

    [Fact]
    public void MultipleSeats_TimeoutAutoFillsActiveAndAdvances()
    {
        var settings = new GameSettings { PickingTimeoutSeconds = 1.0 };
        var engine = CreateEngine(settings);
        engine.JoinSeat(0);
        engine.JoinSeat(1);
        engine.PressDeal(0);

        Assert.Equal(0, engine.ActiveSeatIndex);

        // Timeout seat 0 — auto-fills, enters transition
        engine.Tick(TimeSpan.FromSeconds(2));
        Assert.True(engine.IsTurnTransitioning);
        Assert.True(engine.Seats[0].IsHandComplete);

        // Tick past turn delay — advances to seat 1
        engine.Tick(TimeSpan.FromSeconds(settings.TurnDelaySeconds + 0.1));
        Assert.Equal(GamePhase.Picking, engine.Phase);
        Assert.Equal(1, engine.ActiveSeatIndex);

        // Timeout seat 1 — auto-fills, enters transition
        engine.Tick(TimeSpan.FromSeconds(settings.PickingTimeoutSeconds + 0.1));
        Assert.True(engine.Seats[1].IsHandComplete);

        // Tick past turn delay — all done → Racing
        engine.Tick(TimeSpan.FromSeconds(settings.TurnDelaySeconds + 0.1));
        Assert.Equal(GamePhase.Racing, engine.Phase);
    }

    [Fact]
    public void CannotJoinDuringPicking()
    {
        var engine = CreateEngine();
        engine.JoinSeat(0);
        engine.PressDeal(0); // start dealing

        var result = engine.JoinSeat(1);
        Assert.False(result);
    }

    [Fact]
    public void SelectionCardsAreUnique()
    {
        var engine = CreateEngine();
        engine.JoinSeat(0);
        engine.PressDeal(0);

        var cards = engine.Seats[0].SelectionCards;
        Assert.Equal(5, cards.Length);
        Assert.Equal(5, cards.Distinct().Count());
    }

    [Fact]
    public void PickedCardsDontRepeat()
    {
        var engine = CreateEngine();
        engine.JoinSeat(0);
        engine.PressDeal(0);

        // Pick all 5 cards from different slots
        var allCards = new List<Card>();
        for (int i = 0; i < 5; i++)
        {
            var picked = engine.Seats[0].SelectionCards[i];
            allCards.Add(picked);
            engine.PickCard(0, i);
        }

        // All picked cards should be distinct
        Assert.Equal(5, allCards.Distinct().Count());
        // Hand should contain those cards
        Assert.Equal(5, engine.Seats[0].Hand.Length);
    }
    [Fact]
    public void DisabledPickingTimeout_DoesNotAutoFill()
    {
        var engine = CreateEngine(new GameSettings { PickingTimeoutSeconds = 0 });
        engine.JoinSeat(0);
        engine.PressDeal(0);
        engine.Tick(TimeSpan.FromMinutes(5));
        Assert.Equal(GamePhase.Picking, engine.Phase);
        Assert.Empty(engine.Seats[0].Hand);
        Assert.False(engine.IsTurnTransitioning);
    }

    [Fact]
    public void LeavingLastSeatDuringTransition_ResetsRaceForReplay()
    {
        var engine = CreateEngine();
        engine.JoinSeat(3);
        engine.PressDeal(3);
        PickAllCards(engine, 3);
        Assert.True(engine.IsTurnTransitioning);
        engine.LeaveSeat(3);
        Assert.Equal(GamePhase.Attract, engine.Phase);
        Assert.Equal(0, engine.Round);
        Assert.Equal(0, engine.ActiveSeatIndex);
        Assert.False(engine.IsTurnTransitioning);
        engine.JoinSeat(0);
        engine.PressDeal(0);
        Assert.Equal(1, engine.Round);
        Assert.Empty(engine.Seats[0].Hand);
        Assert.True(engine.PickCard(0, 0));
    }
}
