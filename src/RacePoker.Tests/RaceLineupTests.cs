using RacePoker.Core.Engine;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;
using RacePoker.Core.Services;

namespace RacePoker.Tests;

public class RaceLineupTests
{
    [Theory]
    [InlineData(1, false, 2, true)]
    [InlineData(1, true, 2, true)]
    [InlineData(2, false, 2, false)]
    [InlineData(2, true, 3, true)]
    [InlineData(3, false, 3, false)]
    [InlineData(3, true, 4, true)]
    [InlineData(4, false, 4, false)]
    [InlineData(4, true, 4, false)]
    public void LineupEnforcesSinglePlayerComputerAndFourRacerLimit(int humans, bool computer, int total, bool hasComputer)
    {
        var lineup = new RaceLineup(humans, computer);
        Assert.Equal(total, lineup.RacerCount);
        Assert.Equal(hasComputer, lineup.HasComputer);
        Assert.Equal(humans, lineup.Participants.Count(p => !p.IsComputer));
        Assert.Equal(Enumerable.Range(0, total), lineup.Participants.Select(p => p.SeatId));
        Assert.Equal(total, lineup.Participants.Select(p => p.Name).Distinct().Count());
        Assert.False(lineup.IsComputer(lineup.FirstHumanSeatId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void RejectsUnsupportedHumanCounts(int humans)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new RaceLineup(humans, true));

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    public void EveryParticipantGetsAFiveCardTurnAndScore(int humans, bool computer)
    {
        var lineup = new RaceLineup(humans, computer);
        var engine = new GameEngine(new GameSettings
        {
            MaxSeats = lineup.RacerCount, FinishDistance = 1000000, AutoAdvanceEnabled = false,
            PickingTimeoutSeconds = 0, TurnDelaySeconds = 0.1, RacingDurationSeconds = 0.1
        }, highScores: new HighScoreService(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json")));
        foreach (var player in lineup.Participants) Assert.True(engine.JoinSeat(player.SeatId));
        Assert.True(engine.PressDeal(0));
        foreach (var player in lineup.Participants)
        {
            Assert.Equal(player.SeatId, engine.ActiveSeatIndex);
            for (int i = 0; i < 5; i++) Assert.True(engine.PickCard(player.SeatId, i));
            Assert.True(engine.Seats[player.SeatId].RoundPoints > 0);
            engine.Tick(TimeSpan.FromSeconds(0.2));
        }
        engine.Tick(TimeSpan.FromSeconds(0.2));
        Assert.Equal(GamePhase.RoundEnd, engine.Phase);
        Assert.All(engine.Seats, seat => Assert.True(seat.IsHandComplete));
        Assert.True(engine.PressContinue(lineup.FirstHumanSeatId));
        Assert.Equal(2, engine.Round);
        Assert.Equal(0, engine.ActiveSeatIndex);
        Assert.All(engine.Seats, seat => Assert.Empty(seat.Hand));
    }
}
