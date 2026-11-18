using Godot;
using RacePoker.Core.Enums;
using RacePoker.Core.Services;

namespace RacePoker.Godot;

public partial class Main
{
    private RaceHistoryService _history = null!;
    private Guid _raceId;
    private ComputerDifficulty _raceDifficulty;
    private int _historyCountAtStart;
    private Label _computerHeading = null!;
    private Control _historyLayer = null!;
    private bool _historyOpen => _openDialog != null && _openDialog == _historyLayer;
    private readonly Label[] _historyStats = new Label[4];
    private GridContainer _historyRows = null!;
    private Label _historyNotice = null!, _historyEmpty = null!;
    private Button _historyRetry = null!;

    private void ToggleHistory()
    {
        if (!_historyOpen) RefreshHistory();
        ToggleDialog(_historyLayer);
    }

    private void BuildHistory()
    {
        _historyLayer = new Control { Visible = false, ZIndex = 60 };
        _historyLayer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_historyLayer);
        var shade = new ColorRect { Color = new Color(0, 0, 0, 0.78f), MouseFilter = MouseFilterEnum.Stop };
        shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _historyLayer.AddChild(shade);
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _historyLayer.AddChild(center);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(860, 0) };
        var style = Panel("#0B1D23", "#34545C");
        style.ContentMarginLeft = style.ContentMarginRight = 24;
        style.ContentMarginTop = style.ContentMarginBottom = 22;
        panel.AddThemeStyleboxOverride("panel", style);
        center.AddChild(panel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 16);
        panel.AddChild(column);
        var header = new HBoxContainer();
        var title = Text("Race history", 32, "#F1F8F5");
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        var close = ActionButton("×", ToggleHistory);
        close.CustomMinimumSize = new Vector2(44, 44);
        header.AddChild(close);
        column.AddChild(header);
        column.AddChild(Text("All difficulties · completed races only · draws count separately", 15, "#ADD0C7"));
        var stats = new HBoxContainer();
        stats.AddThemeConstantOverride("separation", 12);
        column.AddChild(stats);
        string[] labels = { "RACES", "WINS", "DRAWS", "BEST SCORE" };
        for (int i = 0; i < labels.Length; i++)
        {
            var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            card.AddThemeStyleboxOverride("panel", Panel("#14312F", "#34564D"));
            stats.AddChild(card);
            var copy = new VBoxContainer();
            card.AddChild(copy);
            copy.AddChild(Text(labels[i], 12, "#ADD0C7"));
            _historyStats[i] = Text("0", 28, i == 3 ? "#F4CF75" : "#77E4BD");
            copy.AddChild(_historyStats[i]);
        }
        column.AddChild(Text("RECENT RACES · LAST 20", 14, "#ADD0C7"));
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 300), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = true
        };
        column.AddChild(scroll);
        var rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(rows);
        _historyEmpty = Text("Finish your first race to see it here.", 18, "#ADD0C7");
        rows.AddChild(_historyEmpty);
        _historyRows = new GridContainer { Columns = 5 };
        _historyRows.AddThemeConstantOverride("h_separation", 20);
        _historyRows.AddThemeConstantOverride("v_separation", 14);
        rows.AddChild(_historyRows);
        _historyNotice = Text("", 14, "#F4CF75");
        _historyNotice.CustomMinimumSize = new Vector2(750, 0);
        _historyNotice.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_historyNotice);
        var footer = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        _historyRetry = ActionButton("Retry saving", () => { _history.Save(); RefreshHistory(); });
        footer.AddChild(_historyRetry);
        footer.AddChild(ActionButton("Back to race · H / Esc", ToggleHistory));
        column.AddChild(footer);
    }

    private void RefreshHistory()
    {
        var data = _history.Data;
        int[] values = { data.TotalRaces, data.Wins, data.Draws, data.BestScore };
        for (int i = 0; i < values.Length; i++) _historyStats[i].Text = values[i].ToString("N0");
        foreach (var child in _historyRows.GetChildren()) { _historyRows.RemoveChild(child); child.QueueFree(); }
        string[] headers = { "WHEN", "DIFFICULTY", "RESULT", "YOU", "COMPUTER" };
        int[] widths = { 190, 90, 70, 110, 110 };
        for (int i = 0; i < headers.Length; i++)
        {
            var label = Text(headers[i], 12, "#ADD0C7");
            label.CustomMinimumSize = new Vector2(widths[i], 0);
            _historyRows.AddChild(label);
        }
        foreach (var entry in data.Recent)
        {
            string outcome = entry.PlayerScore > entry.ComputerScore ? "WIN" : entry.PlayerScore == entry.ComputerScore ? "DRAW" : "LOSS";
            var date = Text(entry.PlayedAt.LocalDateTime.ToString("MMM d · h:mm tt"), 15, "#ADD0C7");
            date.TooltipText = $"{entry.Rounds} rounds · {entry.PlayedAt.LocalDateTime:g}";
            _historyRows.AddChild(date);
            _historyRows.AddChild(Text(entry.Difficulty.ToString(), 15, "#ADD0C7"));
            _historyRows.AddChild(Text(outcome, 15, outcome == "WIN" ? "#77E4BD" : outcome == "DRAW" ? "#F4CF75" : "#F78575"));
            _historyRows.AddChild(Text(entry.PlayerScore.ToString("N0"), 17, "#FFFFFF"));
            _historyRows.AddChild(Text(entry.ComputerScore.ToString("N0"), 17, "#ADD0C7"));
        }
        _historyEmpty.Visible = data.Recent.Count == 0;
        _historyRows.Visible = data.Recent.Count != 0;
        _historyNotice.Text = _history.LastNotice ?? "";
        _historyNotice.Visible = !string.IsNullOrWhiteSpace(_historyNotice.Text);
        _historyRetry.Visible = _history.HasPendingSave;
    }
}
