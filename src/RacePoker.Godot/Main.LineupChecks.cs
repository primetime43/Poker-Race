using Godot;
using RacePoker.Core.Enums;

namespace RacePoker.Godot;

public partial class Main
{
    private bool _lineupCheck;
    private string LineupCheckPrefix => OS.GetCmdlineUserArgs().Contains("--capture-multiplayer") ? "lineup-preview-mixed"
        : OS.GetCmdlineUserArgs().Contains("--capture-four-human") ? "lineup-preview-four" : "lineup-smoke";
    private int _lineupCase;
    private double _lineupCheckTime;
    private bool _setupFormVerified;
    private readonly int[] _humanPicks = new int[4];
    private (int Humans, bool Computer)[] _lineupCases = { (1, false), (2, false), (2, true), (3, false), (3, true), (4, false), (4, true) };

    private void PrepareLineupChecks()
    {
        if (OS.GetCmdlineUserArgs().Contains("--capture-multiplayer")) _lineupCases = new[] { (3, true) };
        if (OS.GetCmdlineUserArgs().Contains("--capture-four-human")) _lineupCases = new[] { (4, false) };
        _preferences.HumanPlayers = _lineupCases[0].Humans;
        _preferences.IncludeComputer = _lineupCases[0].Computer;
    }

    private void CheckLineup(double delta)
    {
        if (!_setupFormVerified)
        {
            ToggleRaceSetup();
            if (_setupComputer.ButtonPressed != _lineup.HasComputer || _setupComputer.Disabled != (_lineup.HumanPlayers is 1 or 4))
            { FailSmoke("Race setup did not enforce required/forbidden computer slots."); return; }
            if (_setupDifficultyPicker.Disabled == _lineup.HasComputer)
            { FailSmoke("Setup difficulty was not enabled only for computer races."); return; }
            if (_lineup.HasComputer)
            {
                var difficulty = (ComputerDifficulty)(_lineupCase % 3);
                _setupDifficultyPicker.Select((int)difficulty);
                _setupDifficultyPicker.EmitSignal(OptionButton.SignalName.ItemSelected, (long)difficulty);
                if (_setupDifficulty != difficulty || !_setupSummary.Text.Contains(difficulty.ToString()))
                { FailSmoke("Race setup did not reflect the selected difficulty."); return; }
                if (PlayerPreferences.Load(_preferencesPath).Difficulty != _preferences.Difficulty)
                { FailSmoke("Changing setup difficulty saved before starting the race."); return; }
                ToggleRaceSetup();
                ToggleRaceSetup();
                if (_setupDifficulty != _preferences.Difficulty)
                { FailSmoke("Canceling setup kept an uncommitted difficulty change."); return; }
                _setupDifficultyPicker.Select((int)difficulty);
                _setupDifficultyPicker.EmitSignal(OptionButton.SignalName.ItemSelected, (long)difficulty);
            }
            _setupFormVerified = true;
            StartFromSetup();
            var savedSetup = PlayerPreferences.Load(_preferencesPath);
            if (savedSetup.HumanPlayers != _lineup.HumanPlayers || savedSetup.IncludeComputer != _lineup.HasComputer
                || savedSetup.Difficulty != _setupDifficulty || _raceDifficulty != _setupDifficulty || _rivalDriver.Difficulty != _setupDifficulty)
            { FailSmoke("Race setup did not save or start the selected lineup."); return; }
            return;
        }
        _lineupCheckTime += delta;
        if (_lineupCheckTime > 30) { FailSmoke("Timed out completing a local-player lineup."); return; }
        if (_track.LaneCount != _lineup.RacerCount || _engine.Seats.Length != _lineup.RacerCount)
        { FailSmoke("Track lanes and core seats did not match the lineup."); return; }
        if (_engine.Phase == GamePhase.Picking && !_lineup.IsComputer(_engine.ActiveSeatIndex) && !_engine.IsTurnTransitioning && !_busy)
        {
            int active = _engine.ActiveSeatIndex;
            if (_currentHumanSeat != active || _choices.Any(c => c.Disabled) || !_selectionContent.Visible)
            { FailSmoke("The active human did not receive card controls."); return; }
            int before = _engine.Seats[active].Hand.Length;
            Pick(before % 5);
            if (_engine.Seats[active].Hand.Length == before + 1) _humanPicks[active]++;
        }
        if (_engine.Phase != GamePhase.GameOver) return;
        foreach (var racer in _lineup.Participants)
        {
            if (!_engine.Seats[racer.SeatId].IsHandComplete || (!racer.IsComputer && _humanPicks[racer.SeatId] != 5))
            { FailSmoke("A human was skipped or did not receive five separate picks."); return; }
        }
        var saved = _history.Data.Recent.FirstOrDefault(e => e.RaceId == _raceId);
        if (_lineup.RacerCount > 4 || (_lineup.HumanPlayers == 1 && !_lineup.HasComputer)
            || (_lineup.HumanPlayers == 4 && _lineup.HasComputer) || _rivalDriver.PicksMade != (_lineup.HasComputer ? 5 : 0)
            || saved == null || saved.Racers.Count != _lineup.RacerCount || saved.HumanPlayers != _lineup.HumanPlayers
            || saved.HasComputer != _lineup.HasComputer || _history.Data.TotalRaces != _historyCountAtStart + _lineupCase + 1
            || !_recapPanel.Visible || _recapRunnerPanels.Count(p => p.Visible) != _lineup.RacerCount)
        { FailSmoke("Lineup constraints, recap, or complete-race history were incorrect."); return; }
        GD.Print($"LINEUP PASS: {_lineup.HumanPlayers} humans, computer={_lineup.HasComputer}, {_lineup.RacerCount} racers; every human picked five cards.");
        if (OS.GetCmdlineUserArgs().Contains("--capture-multiplayer") || OS.GetCmdlineUserArgs().Contains("--capture-four-human"))
        {
            _lineupCheck = _smoke = false;
            CapturePreview(_lineup.HasComputer ? "godot-multiplayer.png" : "godot-four-human.png");
            return;
        }
        _lineupCase++;
        if (_lineupCase == _lineupCases.Length)
        {
            GD.Print("LINEUP SMOKE PASS: all seven requested/normalized lineups completed, with correct lanes, turns, recaps, and history.");
            _audio.Stop();
            GetTree().Quit();
            return;
        }
        _preferences.HumanPlayers = _lineupCases[_lineupCase].Humans;
        _preferences.IncludeComputer = _lineupCases[_lineupCase].Computer;
        _lineupCheckTime = 0;
        _setupFormVerified = false;
        Array.Clear(_humanPicks);
        StartRace();
    }
}
