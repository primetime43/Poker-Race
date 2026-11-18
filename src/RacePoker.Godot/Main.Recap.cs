using Godot;
using RacePoker.Core.Engine;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;

namespace RacePoker.Godot;

public partial class Main
{
    private VBoxContainer _playArea = null!;
    private PanelContainer _recapPanel = null!;
    private GridContainer _recapRunners = null!;
    private Label _recapTitle = null!, _recapSwing = null!, _recapTotals = null!;
    private readonly Label[] _recapNames = new Label[4], _recapPoints = new Label[4], _recapWho = new Label[4];
    private readonly PanelContainer[] _recapRunnerPanels = new PanelContainer[4];
    private readonly TextureRect[][] _recapCards = Enumerable.Range(0, 4).Select(_ => new TextureRect[5]).ToArray();
    private readonly StyleBoxFlat[][] _recapStyles = Enumerable.Range(0, 4).Select(_ => new StyleBoxFlat[5]).ToArray();
    private readonly PanelContainer[][] _recapFrames = Enumerable.Range(0, 4).Select(_ => new PanelContainer[5]).ToArray();
    private readonly StyleBoxFlat[] _handStyles = new StyleBoxFlat[5];
    private int[] _recapSeats = Array.Empty<int>();
    private int _lastRecapRound, _recapsShown;

    private void BuildRoundRecap(Control stage)
    {
        _recapPanel = new PanelContainer { Visible = false };
        _recapPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var outer = Panel("#0D2428", "#38564E");
        outer.ContentMarginLeft = outer.ContentMarginRight = 16;
        outer.ContentMarginTop = outer.ContentMarginBottom = 10;
        _recapPanel.AddThemeStyleboxOverride("panel", outer);
        stage.AddChild(_recapPanel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 5);
        _recapPanel.AddChild(column);
        _recapTitle = Text("ROUND RECAP", 13, "#ADD0C7");
        _recapSwing = Text("", 22, "#F4CF75");
        _recapTotals = Text("", 14, "#ADD0C7");
        column.AddChild(_recapTitle);
        column.AddChild(_recapSwing);
        column.AddChild(_recapTotals);
        _recapRunners = new GridContainer { Columns = 2, SizeFlagsVertical = SizeFlags.ExpandFill };
        _recapRunners.AddThemeConstantOverride("h_separation", 12);
        _recapRunners.AddThemeConstantOverride("v_separation", 8);
        column.AddChild(_recapRunners);
        for (int runner = 0; runner < 4; runner++)
        {
            var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
            var style = Panel("#14312F", "#34564D");
            style.ContentMarginLeft = style.ContentMarginRight = 10;
            style.ContentMarginTop = style.ContentMarginBottom = 6;
            panel.AddThemeStyleboxOverride("panel", style);
            _recapRunnerPanels[runner] = panel;
            _recapRunners.AddChild(panel);
            var contents = new VBoxContainer();
            contents.AddThemeConstantOverride("separation", 4);
            panel.AddChild(contents);
            var heading = new HBoxContainer();
            heading.AddThemeConstantOverride("separation", 12);
            _recapWho[runner] = Text("", 13, "#77E4BD");
            heading.AddChild(_recapWho[runner]);
            _recapNames[runner] = Text("", 15, "#FFFFFF");
            _recapNames[runner].SizeFlagsHorizontal = SizeFlags.ExpandFill;
            heading.AddChild(_recapNames[runner]);
            _recapPoints[runner] = Text("", 18, "#F4CF75");
            heading.AddChild(_recapPoints[runner]);
            contents.AddChild(heading);
            var cards = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsVertical = SizeFlags.ExpandFill };
            cards.AddThemeConstantOverride("separation", 8);
            contents.AddChild(cards);
            for (int i = 0; i < 5; i++)
            {
                var card = new PanelContainer { CustomMinimumSize = new Vector2(84, 112), SizeFlagsVertical = SizeFlags.ShrinkCenter };
                var frame = Panel("#10282B", "#35504E");
                frame.ContentMarginLeft = frame.ContentMarginRight = frame.ContentMarginTop = frame.ContentMarginBottom = 4;
                card.AddThemeStyleboxOverride("panel", frame);
                _recapFrames[runner][i] = card;
                _recapStyles[runner][i] = frame;
                _recapCards[runner][i] = Image(new Vector2(76, 104));
                card.AddChild(_recapCards[runner][i]);
                cards.AddChild(card);
            }
        }
    }

    private void ConfigureRoundRecap()
    {
        _recapSeats = _lineup.Participants.OrderBy(p => p.IsComputer).ThenBy(p => p.SeatId).Select(p => p.SeatId).ToArray();
        bool compact = _lineup.RacerCount > 2;
        for (int runner = 0; runner < 4; runner++)
        {
            _recapRunnerPanels[runner].Visible = runner < _recapSeats.Length;
            if (runner < _recapSeats.Length) _recapWho[runner].Text = _lineup.Name(_recapSeats[runner]);
            for (int i = 0; i < 5; i++)
            {
                _recapFrames[runner][i].CustomMinimumSize = compact ? new Vector2(62, 80) : new Vector2(84, 112);
                _recapCards[runner][i].CustomMinimumSize = compact ? new Vector2(54, 72) : new Vector2(76, 104);
            }
        }
    }

    private void StyleScoringCards(Card[] hand, TextureRect[] images, StyleBoxFlat[] styles)
    {
        bool complete = hand.Length == 5;
        var scoring = complete ? HandEvaluator.GetScoringCardIndices(hand, _engine.Settings.HighCardRule) : Array.Empty<int>();
        for (int i = 0; i < 5; i++)
        {
            bool paid = scoring.Contains(i);
            var style = styles[i];
            style.BorderColor = Color.FromHtml(paid ? "#F4CF75" : "#2B5553");
            style.BorderWidthLeft = style.BorderWidthRight = style.BorderWidthTop = style.BorderWidthBottom = paid ? 3 : 1;
            style.ShadowColor = paid ? new Color(0.96f, 0.8f, 0.4f, 0.2f) : Colors.Transparent;
            style.ShadowSize = paid ? 5 : 0;
            images[i].SelfModulate = complete && !paid ? new Color(0.7f, 0.7f, 0.7f, 1) : Colors.White;
        }
    }

    private void RefreshRoundRecap()
    {
        bool showing = _engine.Phase is GamePhase.RoundEnd or GamePhase.GameOver;
        _recapPanel.Visible = showing;
        _playArea.Visible = !showing;
        if (!showing) return;
        if (_lastRecapRound != _engine.Round)
        {
            _lastRecapRound = _engine.Round;
            _recapsShown++;
            _effects.RevealRecap(_recapPanel);
        }
        _recapTitle.Text = $"ROUND {_engine.Round} RECAP · GOLD OUTLINES MARK SCORING CARDS";
        if (_lineup.RacerCount == 2)
        {
            var human = _engine.Seats[HumanSeat];
            var otherParticipant = _lineup.Participants.First(p => p.SeatId != HumanSeat);
            var other = _engine.Seats[otherParticipant.SeatId];
            int gain = human.RoundPoints - other.RoundPoints;
            _recapSwing.Text = gain == 0 ? "EVEN ROUND · NO CHANGE IN THE GAP"
                : gain > 0 ? $"{_lineup.Name(HumanSeat)} GAINED {gain:N0} PTS ON {otherParticipant.Name}"
                : $"{otherParticipant.Name} GAINED {-gain:N0} PTS ON {_lineup.Name(HumanSeat)}";
        }
        else
        {
            int top = _engine.Seats.Max(s => s.RoundPoints);
            var best = _lineup.Participants.Where(p => _engine.Seats[p.SeatId].RoundPoints == top).Select(p => p.Name);
            _recapSwing.Text = $"BEST ROUND: {string.Join(" / ", best)} · +{top:N0} PTS";
        }
        var leaders = _lineup.Participants.Where(p => _engine.Seats[p.SeatId].TotalScore == _engine.Seats.Max(s => s.TotalScore)).ToArray();
        string lead;
        if (leaders.Length > 1) lead = "Shared lead: " + string.Join(" / ", leaders.Select(p => p.Name));
        else
        {
            var leader = _engine.Seats[leaders[0].SeatId];
            int other = _lineup.Participants.Where(p => p.SeatId != leaders[0].SeatId).Max(p => _engine.Seats[p.SeatId].TotalScore);
            bool tookLead = _lineup.Participants.Any(p => p.SeatId != leaders[0].SeatId && _engine.Seats[p.SeatId].PreviousScore >= leader.PreviousScore);
            lead = tookLead ? $"{leaders[0].Name} took the lead." : $"{leaders[0].Name} leads by {leader.TotalScore - other:N0}.";
        }
        _recapTotals.Text = lead + "  ·  " + string.Join(" · ", _lineup.Participants.Select(p => $"{p.Name} {_engine.Seats[p.SeatId].TotalScore:N0}"));
        for (int runner = 0; runner < _recapSeats.Length; runner++)
        {
            var seat = _engine.Seats[_recapSeats[runner]];
            _recapNames[runner].Text = seat.IsHandComplete ? seat.HandName : "No completed hand";
            _recapPoints[runner].Text = $"+{seat.RoundPoints:N0} pts";
            for (int i = 0; i < 5; i++) _recapCards[runner][i].Texture = i < seat.Hand.Length ? CardTexture(seat.Hand[i]) : null;
            StyleScoringCards(seat.Hand, _recapCards[runner], _recapStyles[runner]);
        }
    }
}
