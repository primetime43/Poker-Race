using Godot;
using RacePoker.Core.Models;

namespace RacePoker.Godot;

public partial class Main
{
    private RaceLineup _lineup = new(1, true);
    private int HumanSeat => _lineup.FirstHumanSeatId;
    private int ComputerSeat => _lineup.ComputerSeatId ?? -1;
    private int _currentHumanSeat;
    private int _lastDealtSeat = -1;
    private readonly int[] _previousScores = new int[4];
    private Control _setupLayer = null!;
    private bool _setupOpen => _openDialog != null && _openDialog == _setupLayer;
    private readonly Button[] _playerCounts = new Button[4];
    private SettingSwitch _setupComputer = null!;
    private Label _setupExplanation = null!, _setupSummary = null!;
    private int _setupHumans = 1;
    private bool _setupAddComputer = true;

    private void ToggleRaceSetup()
    {
        if (!_setupOpen)
        {
            _setupHumans = _preferences.HumanPlayers;
            _setupAddComputer = _preferences.IncludeComputer;
            UpdateRaceSetup();
        }
        ToggleDialog(_setupLayer);
    }

    private void UpdateRaceSetup()
    {
        var preview = new RaceLineup(_setupHumans, _setupAddComputer);
        for (int i = 0; i < 4; i++) _playerCounts[i].SetPressedNoSignal(i + 1 == _setupHumans);
        _setupComputer.SyncState(preview.HasComputer);
        _setupComputer.Disabled = _setupHumans is 1 or 4;
        _setupComputer.Modulate = _setupComputer.Disabled ? new Color(0.65f, 0.65f, 0.65f, 1) : Colors.White;
        _setupExplanation.Text = _setupHumans == 1 ? "Required for single player: you race the computer."
            : _setupHumans == 4 ? "All four places are filled by human players."
            : "Optional. The computer takes one of the four race places.";
        _setupSummary.Text = _setupHumans == 1 ? $"You vs computer · {_preferences.Difficulty} difficulty"
            : $"{_setupHumans} human players{(preview.HasComputer ? $" + computer ({_preferences.Difficulty})" : "")} · {preview.RacerCount} racers";
    }

    private void StartFromSetup()
    {
        var lineup = new RaceLineup(_setupHumans, _setupAddComputer);
        _preferences.HumanPlayers = lineup.HumanPlayers;
        _preferences.IncludeComputer = lineup.HasComputer;
        SavePreferences();
        _setupLayer.Visible = false;
        _openDialog = null;
        StartRace();
    }

    private void BuildRaceSetup()
    {
        _setupLayer = new Control { Visible = false, ZIndex = 60 };
        _setupLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_setupLayer);
        var shade = new ColorRect { Color = new Color(0, 0, 0, 0.8f) };
        shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _setupLayer.AddChild(shade);
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _setupLayer.AddChild(center);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(720, 0) };
        var style = Panel("#0B1D23", "#34545C");
        style.ContentMarginLeft = style.ContentMarginRight = 28;
        style.ContentMarginTop = style.ContentMarginBottom = 24;
        panel.AddThemeStyleboxOverride("panel", style);
        center.AddChild(panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 18);
        panel.AddChild(column);
        var header = new HBoxContainer();
        var title = Text("Set up a race", 34, "#F4CF75");
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        header.AddChild(ActionButton("×", ToggleRaceSetup));
        column.AddChild(header);
        column.AddChild(Text("Play locally. Everyone takes turns using the same card controls.", 16, "#ADD0C7"));
        column.AddChild(Text("HUMAN PLAYERS", 14, "#77E4BD"));
        var counts = new HBoxContainer();
        counts.AddThemeConstantOverride("separation", 10);
        column.AddChild(counts);
        for (int i = 0; i < 4; i++)
        {
            int count = i + 1;
            var button = ActionButton($"{count} player{(count == 1 ? "" : "s")}", () => { _setupHumans = count; UpdateRaceSetup(); });
            button.ToggleMode = true;
            button.CustomMinimumSize = new Vector2(0, 54);
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            button.AddThemeFontSizeOverride("font_size", 19);
            button.AddThemeStyleboxOverride("pressed", Panel("#245447", "#77E4BD"));
            _playerCounts[i] = button;
            counts.AddChild(button);
        }
        var computer = new HBoxContainer();
        var label = Text("Add computer", 23, "#8FC5FF");
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        computer.AddChild(label);
        _setupComputer = SettingToggle("Add computer", true, enabled => { _setupAddComputer = enabled; UpdateRaceSetup(); });
        computer.AddChild(_setupComputer);
        column.AddChild(computer);
        _setupExplanation = Text("", 16, "#ADD0C7");
        column.AddChild(_setupExplanation);
        column.AddChild(new HSeparator());
        _setupSummary = Text("", 20, "#F4CF75");
        column.AddChild(_setupSummary);
        column.AddChild(ActionButton("Start race · Enter", StartFromSetup));
    }

    private void UpdateStandings()
    {
        float score = _track.Score(_currentHumanSeat);
        float leader = _lineup.Participants.Max(p => _track.Score(p.SeatId));
        int place = 1 + _lineup.Participants.Count(p => _track.Score(p.SeatId) > score + 0.5f);
        string prefix = _lineup.HumanPlayers == 1 ? "" : _lineup.Name(_currentHumanSeat) + " · ";
        if (Math.Abs(leader - score) < 0.5f)
        {
            float other = _lineup.Participants.Where(p => p.SeatId != _currentHumanSeat).Max(p => _track.Score(p.SeatId));
            _standing.Text = prefix + (Math.Abs(other - score) < 0.5f ? "JOINT LEAD" : $"1ST PLACE · +{Math.Round(score - other):N0} PTS");
        }
        else _standing.Text = prefix + $"{place}{(place == 2 ? "ND" : place == 3 ? "RD" : "TH")} PLACE · {Math.Round(leader - score):N0} TO CATCH";
        _score.Text = $"{Math.Round(score):N0} / {_engine.Settings.FinishDistance:N0} PTS";
    }
}
