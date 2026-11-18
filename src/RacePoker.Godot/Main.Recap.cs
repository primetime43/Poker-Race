using Godot;
using RacePoker.Core.Engine;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;

namespace RacePoker.Godot;

public partial class Main
{
    private VBoxContainer _playArea = null!;
    private PanelContainer _recapPanel = null!;
    private Label _recapTitle = null!, _recapSwing = null!, _recapTotals = null!;
    private readonly Label[] _recapNames = new Label[2];
    private readonly Label[] _recapPoints = new Label[2];
    private readonly TextureRect[][] _recapCards = { new TextureRect[5], new TextureRect[5] };
    private readonly StyleBoxFlat[][] _recapStyles = { new StyleBoxFlat[5], new StyleBoxFlat[5] };
    private readonly StyleBoxFlat[] _handStyles = new StyleBoxFlat[5];
    private int _lastRecapRound, _recapsShown;

    private void BuildRoundRecap(Control stage)
    {
        _recapPanel = new PanelContainer { Visible = false };
        _recapPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var outer = Panel("#0D2428", "#38564E");
        outer.ContentMarginLeft = outer.ContentMarginRight = 18;
        outer.ContentMarginTop = outer.ContentMarginBottom = 14;
        _recapPanel.AddThemeStyleboxOverride("panel", outer);
        stage.AddChild(_recapPanel);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 9);
        _recapPanel.AddChild(column);
        _recapTitle = Text("ROUND RECAP", 14, "#ADD0C7");
        _recapSwing = Text("", 25, "#F4CF75");
        _recapTotals = Text("", 16, "#ADD0C7");
        column.AddChild(_recapTitle);
        column.AddChild(_recapSwing);
        column.AddChild(_recapTotals);
        var runners = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        runners.AddThemeConstantOverride("separation", 18);
        column.AddChild(runners);
        for (int runner = 0; runner < 2; runner++)
        {
            var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var style = Panel(runner == 0 ? "#14312F" : "#142B37", runner == 0 ? "#345E50" : "#34536A");
            style.ContentMarginLeft = style.ContentMarginRight = 14;
            style.ContentMarginTop = style.ContentMarginBottom = 10;
            panel.AddThemeStyleboxOverride("panel", style);
            runners.AddChild(panel);
            var contents = new VBoxContainer();
            contents.AddThemeConstantOverride("separation", 8);
            panel.AddChild(contents);
            var heading = new HBoxContainer();
            var who = Text(runner == 0 ? "YOU" : "COMPUTER", 14, runner == 0 ? "#77E4BD" : "#8FC5FF");
            who.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            heading.AddChild(who);
            _recapPoints[runner] = Text("", 22, "#F4CF75");
            heading.AddChild(_recapPoints[runner]);
            contents.AddChild(heading);
            _recapNames[runner] = Text("", 18, "#FFFFFF");
            contents.AddChild(_recapNames[runner]);
            var cards = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
            cards.AddThemeConstantOverride("separation", 8);
            contents.AddChild(cards);
            for (int i = 0; i < 5; i++)
            {
                var card = new PanelContainer { CustomMinimumSize = new Vector2(84, 112) };
                var frame = Panel("#10282B", "#35504E");
                frame.ContentMarginLeft = frame.ContentMarginRight = frame.ContentMarginTop = frame.ContentMarginBottom = 4;
                card.AddThemeStyleboxOverride("panel", frame);
                _recapStyles[runner][i] = frame;
                _recapCards[runner][i] = Image(new Vector2(76, 104));
                card.AddChild(_recapCards[runner][i]);
                cards.AddChild(card);
            }
            contents.AddChild(Text("Gold outline = scoring cards", 12, "#ADD0C7"));
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
        var human = _engine.Seats[HumanSeat];
        var computer = _engine.Seats[ComputerSeat];
        if (_lastRecapRound != _engine.Round)
        {
            _lastRecapRound = _engine.Round;
            _recapsShown++;
            _effects.RevealRecap(_recapPanel);
        }
        _recapTitle.Text = $"ROUND {_engine.Round} RECAP";
        int gain = human.RoundPoints - computer.RoundPoints;
        _recapSwing.Text = gain == 0 ? "EVEN ROUND · NO CHANGE IN THE GAP"
            : gain > 0 ? $"YOU GAINED {gain:N0} PTS ON THE COMPUTER" : $"COMPUTER GAINED {-gain:N0} PTS ON YOU";
        int previousLead = human.PreviousScore - computer.PreviousScore;
        int lead = human.TotalScore - computer.TotalScore;
        string change = lead == 0 ? "The race is tied." : previousLead <= 0 && lead > 0 ? "You took the lead."
            : previousLead >= 0 && lead < 0 ? "Computer took the lead." : lead > 0 ? $"You lead by {lead:N0}." : $"Computer leads by {-lead:N0}.";
        _recapTotals.Text = $"{change}   Race totals: you {human.TotalScore:N0} · computer {computer.TotalScore:N0}";
        var seats = new[] { human, computer };
        for (int runner = 0; runner < 2; runner++)
        {
            var seat = seats[runner];
            _recapNames[runner].Text = seat.IsHandComplete ? seat.HandName : "No completed hand";
            _recapPoints[runner].Text = $"+{seat.RoundPoints:N0} pts";
            for (int i = 0; i < 5; i++)
                _recapCards[runner][i].Texture = i < seat.Hand.Length ? CardTexture(seat.Hand[i]) : null;
            StyleScoringCards(seat.Hand, _recapCards[runner], _recapStyles[runner]);
        }
    }
}
