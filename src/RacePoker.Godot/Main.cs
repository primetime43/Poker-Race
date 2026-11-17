using Godot;
using RacePoker.Core.Engine;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;
using RacePoker.Core.Services;

namespace RacePoker.Godot;

public partial class Main : Control
{
    private const int ComputerSeat = 0;
    private const int HumanSeat = 1;
    private readonly Button[] _choices = new Button[5];
    private readonly TextureRect[] _choiceImages = new TextureRect[5];
    private readonly TextureRect[] _handImages = new TextureRect[5];
    private readonly Dictionary<string, Texture2D> _textures = new();
    private GameEngine _engine = null!;
    private RaceTrack _track = null!;
    private ArcadeEffects _effects = null!;
    private ArcadeAudio _audio = null!;
    private RivalDriver _rivalDriver = new();
    private Label _standing = null!;
    private Label _computerSummary = null!;
    private Label _handTitle = null!, _watchComputer = null!;
    private VBoxContainer _selectionContent = null!;
    private int _displayedSeat = -1, _displayedHandCount;
    private Label _round = null!, _score = null!, _status = null!, _hint = null!;
    private ProgressBar _timer = null!;
    private Button _continue = null!, _pause = null!, _mute = null!;
    private bool _busy, _dealPending;
    private int _lastRound;
    private bool _paused;
    private bool _smoke;
    private double _smokeTime, _nextPick;
    private int _smokeHands;
    private bool _smokePauseStarted, _smokePauseChecked;
    private int _pausedFrameChanges;
    private float _pausedScore;
    private float _pausedCardFlight;
    private float _pausedRivalScore;
    private int _pausedRivalFrames, _previousRivalScore;
    private double _rivalPauseStart;
    private bool _rivalPauseChecked;
    private int _pausedRivalPicks;
    private GamePhase _previousPhase;
    private int _previousScore;

    public override void _Ready()
    {
        _smoke = OS.GetCmdlineUserArgs().Contains("--smoke-test") || OS.GetCmdlineUserArgs().Contains("--capture-results")
            || OS.GetCmdlineUserArgs().Contains("--capture-player-turn");
        BuildScreen();
        _effects = new ArcadeEffects();
        _effects.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_effects);
        _audio = new ArcadeAudio();
        AddChild(_audio);
        StartRace();
        if (OS.GetCmdlineUserArgs().Contains("--capture-preview")) CapturePreview("godot.png", 3.3);
    }

    private async void CapturePreview(string filename = "godot.png", double delay = 1)
    {
        await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        string path = ProjectSettings.GlobalizePath("res://../../artifacts/previews/" + filename);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        var error = GetViewport().GetTexture().GetImage().SavePng(path);
        GD.Print($"Preview: {path} ({error})");
        GetTree().Quit(error == Error.Ok ? 0 : 1);
    }

    private void StartRace()
    {
        if (_engine != null) _engine.StateChanged -= Refresh;
        _effects.Clear();
        _audio.Stop();
        _audio.SetPaused(false);
        _lastRound = 0;
        _previousRivalScore = 0;
        _rivalDriver = new RivalDriver();
        _busy = _dealPending = false;
        _displayedSeat = -1;
        _displayedHandCount = 0;
        foreach (var image in _choiceImages.Concat(_handImages))
        {
            image.Scale = Vector2.One;
            image.Rotation = 0;
            image.Modulate = Colors.White;
        }
        // This experiment never reads or writes the WPF game's leaderboard.
        _engine = new GameEngine(new GameSettings
        {
            MaxSeats = 2, MaxRounds = _smoke ? 2 : 10, FinishDistance = 150000,
            PickingTimeoutSeconds = _smoke ? 0 : 30,
            TurnDelaySeconds = 1.5, RacingDurationSeconds = 0.5,
            AutoAdvanceEnabled = false, GameOverTimeoutSeconds = 300
        }, rng: _smoke ? new SmokeRng() : null,
            highScores: new HighScoreService(ProjectSettings.GlobalizePath("user://prototype-scores.json")));
        _paused = false;
        _pause.Text = "Pause · P";
        _previousScore = 0;
        _previousPhase = GamePhase.Attract;
        _track.ResetRace();
        _engine.StateChanged += Refresh;
        _engine.JoinSeat(0);
        _engine.JoinSeat(1);
        _engine.PressDeal(0);
        Refresh();
    }

    public override void _Process(double delta)
    {
        if (_dealPending)
        {
            _dealPending = false;
            _effects.Deal(_choiceImages, () => { _busy = false; Refresh(); });
        }
        if (!_paused)
        {
            _engine.Tick(TimeSpan.FromSeconds(delta));
            _rivalDriver.Tick(_engine, delta);
        }
        _score.Text = $"{Math.Round(_track.DisplayScore):N0}  /  {_engine.Settings.FinishDistance:N0} PTS";
        float lead = _track.DisplayScore - _track.RivalDisplayScore;
        _standing.Text = Math.Abs(lead) < 0.5f ? "NECK AND NECK" : lead > 0
            ? $"1ST PLACE   +{Math.Round(lead):N0} PTS" : $"2ND PLACE   {Math.Round(-lead):N0} PTS TO CATCH";
        _timer.Value = _engine.PhaseTimerProgress * 100;
        if (!_smoke) return;
        if (OS.GetCmdlineUserArgs().Contains("--capture-player-turn") && _engine.ActiveSeatIndex == HumanSeat && !_busy)
        {
            _smoke = false;
            CapturePreview("godot-player-turn.png");
            return;
        }
        _smokeTime += delta;
        if (_smokeTime > 40) { FailSmoke("Timed out before completing two rounds against the rival."); return; }
        if (!_smokePauseStarted && _smokeTime >= 0.9)
        {
            TogglePause();
            _pausedFrameChanges = _track.AnimationFrameChanges;
            _pausedScore = _track.DisplayScore;
            _pausedCardFlight = _effects.CardFlightProgress;
            _pausedRivalScore = _track.RivalDisplayScore;
            _pausedRivalFrames = _track.RivalAnimationFrameChanges;
            _smokePauseStarted = true;
            return;
        }
        if (_smokePauseStarted && !_smokePauseChecked)
        {
            if (_smokeTime < 1.2) return;
            if (_track.AnimationFrameChanges != _pausedFrameChanges || _track.DisplayScore != _pausedScore
                || _effects.CardFlightProgress != _pausedCardFlight || _track.RivalDisplayScore != _pausedRivalScore
                || _track.RivalAnimationFrameChanges != _pausedRivalFrames)
            { FailSmoke("Horse or card animation continued while paused."); return; }
            TogglePause();
            _smokePauseChecked = true;
        }
        if (!_rivalPauseChecked && _rivalPauseStart == 0 && _track.RivalDisplayScore > 0 && _engine.IsTurnTransitioning)
        {
            TogglePause();
            _rivalPauseStart = _smokeTime;
            _pausedRivalScore = _track.RivalDisplayScore;
            _pausedRivalFrames = _track.RivalAnimationFrameChanges;
            _pausedRivalPicks = _rivalDriver.PicksMade;
            return;
        }
        if (!_rivalPauseChecked && _rivalPauseStart > 0)
        {
            if (_smokeTime - _rivalPauseStart < 0.3) return;
            if (_track.RivalDisplayScore != _pausedRivalScore || _track.RivalAnimationFrameChanges != _pausedRivalFrames
                || _rivalDriver.PicksMade != _pausedRivalPicks)
            { FailSmoke("Rival movement or decision making continued while paused."); return; }
            TogglePause();
            _rivalPauseChecked = true;
        }
        if (_engine.Phase == GamePhase.Picking && _engine.ActiveSeatIndex == ComputerSeat)
        {
            if (_choices.Any(c => !c.Disabled) || _selectionContent.Visible || _handTitle.Text != "COMPUTER'S HAND"
                || _engine.Seats[HumanSeat].Hand.Length != 0
                || _handImages.Count(c => c.Texture != null) != _engine.Seats[ComputerSeat].Hand.Length)
            { FailSmoke("Computer's opening hand was not shown alone at full size."); return; }
        }
        if (_engine.Phase == GamePhase.Picking && _engine.ActiveSeatIndex == HumanSeat
            && (!_engine.Seats[ComputerSeat].IsHandComplete || !_selectionContent.Visible || _handTitle.Text != "YOUR HAND"))
        { FailSmoke("Player selection did not follow the completed computer hand."); return; }
        if (_engine.Phase == GamePhase.Picking && _engine.ActiveSeatIndex == HumanSeat && !_engine.IsTurnTransitioning && _smokeTime >= _nextPick)
        {
            _nextPick = _smokeTime + 0.12;
            // Prefer a matching rank to exercise a meaningful payout and horse movement.
            var seat = _engine.Seats[HumanSeat];
            int index = Array.FindIndex(seat.SelectionCards, c => seat.Hand.Any(h => h.Rank == c.Rank));
            index = Math.Max(0, index);
            // Exercise both the actual button wiring and keyboard input path.
            if (seat.Hand.Length % 2 == 0) _choices[index].EmitSignal(Button.SignalName.Pressed);
            else _UnhandledKeyInput(new InputEventKey { Pressed = true, Keycode = Key.Key1 + index });
        }
        if (_engine.Phase == GamePhase.RoundEnd) Continue();
        if (_engine.Phase == GamePhase.GameOver)
        {
            if (_smokeHands != 2 || _engine.Seats[HumanSeat].TotalScore <= 0 || _track.DisplayScore <= 0 || _track.AnimationFrameChanges < 16
                || _effects.CardsFlown != 10 || _effects.RewardsShown != 4 || _effects.CelebrationsShown != 1
                || _rivalDriver.PicksMade != 10 || _engine.Seats[ComputerSeat].TotalScore <= 0 || _track.RivalDisplayScore <= 0 || !_rivalPauseChecked
                || _effects.ComputerCardsRevealed != 10)
                FailSmoke("Expected two rounds, ten picks per runner, four payouts, two running horses, and a celebration.");
            else
            {
                GD.Print($"SMOKE PASS: {_smokeHands} rounds, YOU {_engine.Seats[HumanSeat].TotalScore} vs RIVAL {_engine.Seats[ComputerSeat].TotalScore}, 10 rival picks, {_effects.CardsFlown} card flights, {_effects.RewardsShown} payouts; both horses pause correctly.");
                if (OS.GetCmdlineUserArgs().Contains("--capture-results"))
                {
                    _smoke = false;
                    CapturePreview("godot-results.png");
                }
                else VerifyRestartAndQuit();
            }
        }
    }

    private void FailSmoke(string message)
    {
        GD.PushError("SMOKE FAIL: " + message);
        GetTree().Quit(1);
    }

    private async void VerifyRestartAndQuit()
    {
        _smoke = false;
        StartRace();
        if (!await WaitForHumanTurn()) return;
        Pick(0);
        await ToSignal(GetTree().CreateTimer(0.08), SceneTreeTimer.SignalName.Timeout);
        StartRace(); // Interrupt a card in mid-flight; old callbacks must not affect the new race.
        if (_engine.Seats[HumanSeat].Hand.Length != 0 || _effects.CardsFlown != 0 || _track.DisplayScore != 0
            || _track.RivalDisplayScore != 0 || _rivalDriver.PicksMade != 0 || _busy)
        { FailSmoke("Restart did not clear an interrupted card flight."); return; }
        if (!await WaitForHumanTurn()) return;
        Pick(1);
        await ToSignal(GetTree().CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);
        if (_engine.Seats[HumanSeat].Hand.Length != 1 || _effects.CardsFlown != 1 || _busy || _handImages[0].Modulate.A < 0.99f)
        { FailSmoke("Card selection did not recover after restart."); return; }
        ToggleMute();
        if (!_audio.Muted) { FailSmoke("Mute toggle failed."); return; }
        ToggleMute();
        GD.Print("SMOKE PASS: mid-flight restart, subsequent selection, and sound toggle verified.");
        _audio.Stop();
        GetTree().Quit();
    }

    private async Task<bool> WaitForHumanTurn()
    {
        for (int i = 0; i < 100; i++)
        {
            if (_engine.ActiveSeatIndex == HumanSeat && !_busy) return true;
            await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
        }
        FailSmoke("Computer did not finish its opening turn.");
        return false;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        int index = key.Keycode switch
        {
            Key.Key1 or Key.F1 => 0, Key.Key2 or Key.F2 => 1, Key.Key3 or Key.F3 => 2,
            Key.Key4 or Key.F4 => 3, Key.Key5 or Key.F5 => 4, _ => -1
        };
        if (index >= 0) Pick(index);
        else if (key.Keycode is Key.Enter or Key.KpEnter) Continue();
        else if (key.Keycode == Key.P) TogglePause();
        else if (key.Keycode == Key.M) ToggleMute();
        else if (key.Keycode == Key.F11)
            DisplayServer.WindowSetMode(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen
                ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen);
        else return;
        GetViewport().SetInputAsHandled();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut && !_smoke && _engine != null && !_paused)
            TogglePause();
    }

    private void Pick(int index)
    {
        if (_paused || _busy || index < 0 || index >= 5) return;
        int slot = _engine.Seats[HumanSeat].Hand.Length;
        var texture = _choiceImages[index].Texture;
        var source = _choiceImages[index].GetGlobalRect();
        if (texture == null || !_engine.PickCard(HumanSeat, index)) return;
        _busy = true;
        _audio.Pick(slot);
        _effects.Hover(_choiceImages[index], false);
        _effects.FlyCard(texture, source, _handImages[slot], () => { _busy = false; Refresh(); });
        Refresh();
    }

    private void Continue()
    {
        if (_paused || _busy) return;
        if (_engine.Phase == GamePhase.GameOver) StartRace();
        else _engine.PressContinue(HumanSeat);
    }

    private void TogglePause()
    {
        _paused = !_paused;
        _track.SetPaused(_paused);
        _effects.SetPaused(_paused);
        _audio.SetPaused(_paused);
        _pause.Text = _paused ? "Resume · P" : "Pause · P";
        Refresh();
    }

    private void ToggleMute()
    {
        _audio.ToggleMute();
        _mute.Text = _audio.Muted ? "Sound off · M" : "Sound on · M";
    }

    private void Refresh()
    {
        var seat = _engine.Seats[HumanSeat];
        var rival = _engine.Seats[ComputerSeat];
        _track.SetRacing(_engine.Phase is GamePhase.Picking or GamePhase.Racing);
        bool rivalTurn = _engine.Phase == GamePhase.Picking && _engine.ActiveSeatIndex == ComputerSeat;
        if (_engine.Round > 0 && _engine.Round != _lastRound && _engine.ActiveSeatIndex == HumanSeat)
        {
            _lastRound = _engine.Round;
            _busy = _dealPending = true;
        }
        bool picking = _engine.Phase == GamePhase.Picking && _engine.ActiveSeatIndex == HumanSeat && !_engine.IsTurnTransitioning;
        _round.Text = $"ROUND {Math.Max(1, _engine.Round):00} / {_engine.Settings.MaxRounds:00}";
        _status.Text = _paused ? "Race paused" : _engine.Phase == GamePhase.GameOver ? ResultTitle()
            : rivalTurn ? rival.IsHandComplete ? $"COMPUTER: {rival.HandName.ToUpperInvariant()}   +{rival.RoundPoints:N0} PTS" : $"COMPUTER IS THINKING   {rival.Hand.Length} / 5" : seat.IsHandComplete
            ? $"{seat.HandName.ToUpperInvariant()}   +{seat.RoundPoints:N0} PTS"
            : $"BUILD YOUR HAND   {seat.Hand.Length} / 5";
        _hint.Text = _paused ? "Press P or Resume to keep playing."
            : _engine.Phase == GamePhase.GameOver ? $"You: {seat.TotalScore:N0} pts   ·   Computer: {rival.TotalScore:N0} pts   ·   Enter to race again."
            : rivalTurn ? "The computer goes first. Watch its hand, then build yours."
            : seat.IsHandComplete ? "Your hand is scored. Watch your horse advance."
            : "Pick one of the five cards below. A new card replaces every pick.";
        _continue.Visible = _engine.Phase is GamePhase.RoundEnd or GamePhase.GameOver;
        _continue.Disabled = _paused || _busy;
        _continue.Text = _engine.Phase == GamePhase.GameOver ? "Race again · Enter" : "Next hand · Enter";
        _timer.Visible = picking;
        _computerSummary.Text = rival.IsHandComplete ? $"{rival.HandName} · +{rival.RoundPoints:N0} pts"
            : rivalTurn ? $"Choosing cards · {rival.Hand.Length}/5" : "Computer plays first";
        _handTitle.Text = rivalTurn ? "COMPUTER'S HAND" : "YOUR HAND";
        _handTitle.Modulate = rivalTurn ? Color.FromHtml("#8FC5FF") : Colors.White;
        _selectionContent.Visible = !rivalTurn;
        _watchComputer.Visible = rivalTurn;
        _watchComputer.Text = rival.IsHandComplete ? "Your turn is next…" : "Computer is choosing its cards…";
        int shownSeat = rivalTurn ? ComputerSeat : HumanSeat;
        var shownHand = _engine.Seats[shownSeat].Hand;
        if (_displayedSeat != shownSeat)
        {
            _displayedSeat = shownSeat;
            _displayedHandCount = 0;
            foreach (var image in _handImages) { image.Scale = Vector2.One; image.Modulate = Colors.White; }
        }
        for (int i = 0; i < 5; i++)
        {
            _choices[i].Disabled = !picking || _paused || _busy;
            _choiceImages[i].Texture = i < seat.SelectionCards.Length ? CardTexture(seat.SelectionCards[i]) : null;
            _handImages[i].Texture = i < shownHand.Length ? CardTexture(shownHand[i]) : null;
        }
        if (rivalTurn && shownHand.Length > _displayedHandCount)
            for (int i = _displayedHandCount; i < shownHand.Length; i++)
                _effects.RevealComputerCard(_handImages[i]);
        _displayedHandCount = shownHand.Length;
        if (seat.TotalScore != _previousScore)
        {
            _track.MoveToScore(seat.TotalScore, _engine.Settings.FinishDistance);
            _effects.Reward(seat.HandName, seat.RoundPoints, _track.GetGlobalRect());
            _audio.Reward();
            _previousScore = seat.TotalScore;
        }
        if (rival.TotalScore != _previousRivalScore)
        {
            _track.MoveRivalToScore(rival.TotalScore, _engine.Settings.FinishDistance);
            _effects.Reward("COMPUTER · " + rival.HandName, rival.RoundPoints, _track.GetGlobalRect());
            _previousRivalScore = rival.TotalScore;
        }
        if (_engine.Phase == GamePhase.GameOver && _previousPhase != GamePhase.GameOver)
        {
            _effects.Celebrate();
            _audio.Finish();
        }
        if (_smoke && _engine.Phase == GamePhase.Racing && _previousPhase != GamePhase.Racing)
            _smokeHands++;
        _previousPhase = _engine.Phase;
    }

    private string ResultTitle()
    {
        int difference = _engine.Seats[HumanSeat].TotalScore - _engine.Seats[ComputerSeat].TotalScore;
        return difference == 0 ? "DEAD HEAT — SHARED FIRST PLACE"
            : difference > 0 ? $"YOU WIN   BY {difference:N0} PTS" : $"COMPUTER WINS   BY {-difference:N0} PTS";
    }

    private Texture2D CardTexture(Card card)
    {
        if (!_textures.TryGetValue(card.ImageFileName, out var texture))
            _textures[card.ImageFileName] = texture = GD.Load<Texture2D>("res://Assets/PokerCards/" + card.ImageFileName);
        return texture;
    }

    private void BuildScreen()
    {
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (var side in new[] { "left", "top", "right", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 24);
        AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10);
        margin.AddChild(column);
        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 24);
        column.AddChild(top);
        var brand = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        brand.AddThemeConstantOverride("separation", 12);
        top.AddChild(brand);
        var title = Text("RACE POKER", 36, "#F4CF75");
        brand.AddChild(title);
        _standing = Text("NECK AND NECK", 18, "#77E4BD");
        brand.AddChild(_standing);
        // The header keeps the computer's result; its cards use the main hand area.
        var computerPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(210, 0), SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        computerPanel.AddThemeStyleboxOverride("panel", Panel("#0B202B", "#264251"));
        top.AddChild(computerPanel);
        var raceInfo = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        raceInfo.AddThemeConstantOverride("separation", 12);
        top.AddChild(raceInfo);
        var header = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        raceInfo.AddChild(header);
        _round = Text("", 18, "#ADD0C7");
        header.AddChild(_round);
        _pause = ActionButton("Pause · P", TogglePause);
        header.AddChild(_pause);
        _mute = ActionButton("Sound on · M", ToggleMute);
        header.AddChild(_mute);
        var restart = ActionButton("New race", StartRace);
        header.AddChild(restart);
        _score = Text("", 22, "#F4CF75");
        _score.HorizontalAlignment = HorizontalAlignment.Right;
        raceInfo.AddChild(_score);
        _track = new RaceTrack { CustomMinimumSize = new Vector2(0, 250) };
        column.AddChild(_track);
        _status = Text("", 25, "#F4CF75");
        column.AddChild(_status);
        _hint = Text("", 17, "#ADD0C7");
        column.AddChild(_hint);
        _handTitle = Text("COMPUTER'S HAND", 14, "#77E4BD");
        column.AddChild(_handTitle);
        var handArea = new HBoxContainer();
        handArea.AddThemeConstantOverride("separation", 24);
        column.AddChild(handArea);
        var hand = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        hand.AddThemeConstantOverride("separation", 14);
        handArea.AddChild(hand);
        for (int i = 0; i < 5; i++)
        {
            var panel = new PanelContainer { CustomMinimumSize = new Vector2(100, 120) };
            panel.AddThemeStyleboxOverride("panel", Panel("#102E32", "#2B5553"));
            _handImages[i] = Image(new Vector2(88, 108));
            panel.AddChild(_handImages[i]);
            hand.AddChild(panel);
        }
        var computer = new VBoxContainer();
        computer.AddThemeConstantOverride("separation", 5);
        computerPanel.AddChild(computer);
        computer.AddChild(Text("COMPUTER OPPONENT", 13, "#8FC5FF"));
        _computerSummary = Text("", 12, "#93ADBE");
        _computerSummary.CustomMinimumSize = new Vector2(180, 0);
        _computerSummary.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        computer.AddChild(_computerSummary);
        var selectionStage = new Control { CustomMinimumSize = new Vector2(0, 202) };
        column.AddChild(selectionStage);
        _selectionContent = new VBoxContainer();
        _selectionContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _selectionContent.AddThemeConstantOverride("separation", 10);
        selectionStage.AddChild(_selectionContent);
        _watchComputer = Text("Computer is choosing its cards…", 20, "#8FC5FF");
        _watchComputer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _watchComputer.HorizontalAlignment = HorizontalAlignment.Center;
        selectionStage.AddChild(_watchComputer);
        var choiceHeader = new HBoxContainer();
        _selectionContent.AddChild(choiceHeader);
        var choose = Text("PICK A CARD", 14, "#77E4BD");
        choose.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        choiceHeader.AddChild(choose);
        _timer = new ProgressBar { CustomMinimumSize = new Vector2(240, 10), ShowPercentage = false, MaxValue = 100 };
        choiceHeader.AddChild(_timer);
        var choices = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        choices.AddThemeConstantOverride("separation", 18);
        _selectionContent.AddChild(choices);
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            var stack = new VBoxContainer();
            choices.AddChild(stack);
            _choices[i] = ActionButton("", () => Pick(index));
            _choices[i].CustomMinimumSize = new Vector2(132, 150);
            // Use keyboard shortcuts without a focused button consuming Enter or Space.
            _choices[i].FocusMode = FocusModeEnum.None;
            _choices[i].MouseEntered += () => { if (!_choices[index].Disabled) _effects.Hover(_choiceImages[index], true); };
            _choices[i].MouseExited += () => _effects.Hover(_choiceImages[index], false);
            stack.AddChild(_choices[i]);
            _choiceImages[i] = Image(Vector2.Zero);
            _choices[i].AddChild(_choiceImages[i]);
            _choiceImages[i].SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _choiceImages[i].OffsetLeft = _choiceImages[i].OffsetTop = 7;
            _choiceImages[i].OffsetRight = _choiceImages[i].OffsetBottom = -7;
            var number = Text($"{i + 1}", 16, "#ADD0C7");
            number.HorizontalAlignment = HorizontalAlignment.Center;
            stack.AddChild(number);
        }
        var footer = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.End };
        column.AddChild(footer);
        var keys = Text("CLICK OR 1–5 TO PICK     •     P PAUSE     •     F11 FULLSCREEN", 14, "#ADD0C7");
        keys.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        footer.AddChild(keys);
        _continue = ActionButton("Next hand · Enter", Continue);
        footer.AddChild(_continue);
    }

    private static Label Text(string value, int size, string color)
    {
        var label = new Label { Text = value, VerticalAlignment = VerticalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", Color.FromHtml(color));
        return label;
    }

    private static TextureRect Image(Vector2 size) => new()
    {
        CustomMinimumSize = size, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore
    };

    private static StyleBoxFlat Panel(string fill, string border) => new()
    {
        BgColor = Color.FromHtml(fill), BorderColor = Color.FromHtml(border),
        BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
        CornerRadiusTopLeft = 9, CornerRadiusTopRight = 9, CornerRadiusBottomLeft = 9, CornerRadiusBottomRight = 9,
        ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8
    };

    private static Button ActionButton(string title, Action action)
    {
        var button = new Button { Text = title, MouseDefaultCursorShape = CursorShape.PointingHand, FocusMode = FocusModeEnum.None };
        button.AddThemeStyleboxOverride("normal", Panel("#15383B", "#39625E"));
        button.AddThemeStyleboxOverride("hover", Panel("#24504C", "#F4CF75"));
        button.AddThemeStyleboxOverride("pressed", Panel("#38645B", "#F4CF75"));
        button.AddThemeStyleboxOverride("disabled", Panel("#10272B", "#234442"));
        button.Pressed += action;
        return button;
    }

    private sealed class SmokeRng : IRng
    {
        private readonly Random _random = new(42);
        public int Next(int maxExclusive) => _random.Next(maxExclusive);
    }
}
