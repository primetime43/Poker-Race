namespace RacePoker.Core.Models;

public record RaceParticipant(int SeatId, string Name, bool IsComputer);

public sealed class RaceLineup
{
    public int HumanPlayers { get; }
    public bool HasComputer { get; }
    public int RacerCount => Participants.Count;
    public int? ComputerSeatId => HasComputer ? 0 : null;
    public IReadOnlyList<RaceParticipant> Participants { get; }
    public int FirstHumanSeatId => HasComputer ? 1 : 0;

    public RaceLineup(int humanPlayers, bool includeComputer)
    {
        if (humanPlayers is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(humanPlayers));
        HumanPlayers = humanPlayers;
        HasComputer = humanPlayers == 1 || (humanPlayers < 4 && includeComputer);
        var participants = new List<RaceParticipant>();
        if (HasComputer) participants.Add(new RaceParticipant(0, "COMPUTER", true));
        for (int i = 0; i < humanPlayers; i++)
            participants.Add(new RaceParticipant(participants.Count, humanPlayers == 1 ? "YOU" : $"PLAYER {i + 1}", false));
        Participants = participants.AsReadOnly();
    }

    public bool IsComputer(int seatId) => HasComputer && seatId == 0;
    public string Name(int seatId) => Participants.First(p => p.SeatId == seatId).Name;
}
