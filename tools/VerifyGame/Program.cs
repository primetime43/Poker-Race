using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RacePoker.Core.Engine;
using RacePoker.Core.Enums;
using RacePoker.Core.Models;
using RacePoker.Core.Services;
using RacePoker.Wpf;
using RacePoker.Wpf.Controls;
using RacePoker.Wpf.ViewModels;
using RacePoker.Wpf.Views;

class Program
{
    static string Output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/previews"));
    static void Assert(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS " + message); }
    [STAThread]
    static void Main()
    {
        Directory.CreateDirectory(Output);
        var errors = new StringWriter();
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        PresentationTraceSources.DataBindingSource.Listeners.Add(new TextWriterTraceListener(errors));
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/RacePoker.Wpf;component/Themes/CabinetTheme.xaml", UriKind.Relative) });
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/RacePoker.Wpf;component/Themes/ArcadeTheme.xaml", UriKind.Relative) });
        var settings = new GameSettings { SoundEnabled = false, MaxRounds = 1, TurnDelaySeconds = 0.01, RacingDurationSeconds = 0.01, AutoAdvanceEnabled = false, PickingTimeoutSeconds = 30 };
        var data = Path.Combine(Output, "data-" + Guid.NewGuid().ToString("N"));
        var vm = new MainViewModel(settings, data);
        var timer = (DispatcherTimer)typeof(MainViewModel).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
        timer.Stop();
        var engine = (GameEngine)typeof(MainViewModel).GetField("_engine", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
        var shell = new ShellView { DataContext = vm };
        shell.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        Assert(((RotateTransform)shell.FindName("CardRotate1")).HasAnimatedProperties, "lobby sway starts when loaded");
        settings.CardSwayDegrees = 0; settings.LogoGlowBlur = 0;
        typeof(MainViewModel).GetMethod("SettingsApplied", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, null);
        Assert(!((RotateTransform)shell.FindName("CardRotate1")).HasAnimatedProperties, "saving zero sway stops the existing animation");
        Assert(!((System.Windows.Media.Effects.DropShadowEffect)shell.FindName("LogoGlow")).HasAnimatedProperties, "saving zero glow stops the existing animation");
        settings.CardSwayDegrees = 10; settings.LogoGlowBlur = 30;
        typeof(MainViewModel).GetMethod("SettingsApplied", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, null);
        Assert(((RotateTransform)shell.FindName("CardRotate1")).HasAnimatedProperties, "lobby animation can be restarted without reloading the view");
        Render(shell, "lobby-empty.png");
        for (int i = 0; i < 10; i++) vm.HighScores.Add(new HighScoreEntry { Initials = i == 0 ? "ACE" : "P" + i, Score = 81140 - i * 2200, Date = new DateTime(2026, 10, 7) });
        Render(shell, "lobby.png");
        vm.HandleKeyDown(Key.D4);
        Assert(engine.Seats.Count(s => s.Status == SeatStatus.Joined) == 4, "four-player start");
        var game = new RacePokerGameView { DataContext = vm };
        Render(game, "four-player-picking.png");
        engine.Tick(TimeSpan.FromSeconds(12));
        typeof(MainViewModel).GetField("_lastTick", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(vm, TimeProvider.System.GetTimestamp());
        typeof(MainViewModel).GetMethod("OnTimerTick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, new object?[] { null, EventArgs.Empty });
        Assert(vm.TimerRemainingFraction < 0.61 && vm.TimerRemainingFraction > 0.5, "countdown shrinks as time passes (remaining fraction: " + vm.TimerRemainingFraction + ")");
        Assert(vm.CountdownDisplay == "18s to pick", "countdown displays remaining seconds");
        vm.HandleKeyDown(Key.D1); vm.HandleKeyDown(Key.D2); vm.HandleKeyDown(Key.D3);
        Assert(vm.HandProgressDisplay.StartsWith("3 / 5"), "hand progress after three picks");
        Render(game, "four-player-hand.png");
        vm.HandleKeyDown(Key.D4); vm.HandleKeyDown(Key.D5);
        Assert(engine.IsTurnTransitioning, "completed hand animates before next turn");
        Assert(!vm.ShowSelectionRow, "selection hides during scored-hand transition");
        vm.HandleKeyDown(Key.D1);
        Assert(engine.Seats[0].Hand.Length == 5, "extra picks ignored after hand completion");
        engine.Tick(TimeSpan.FromSeconds(0.1));
        Assert(engine.ActiveSeatIndex == 1, "turn passes to player two");
        Assert(vm.TurnInstruction.StartsWith("Player 2:"), "turn instruction identifies the next player");
        for (int seat = 1; seat < 4; seat++)
        {
            for (int card = 0; card < 5; card++) vm.HandleKeyDown(Key.D1);
            engine.Tick(TimeSpan.FromSeconds(0.1));
        }
        Assert(engine.Phase == GamePhase.Racing, "all four hands advance to race standings");
        engine.Tick(TimeSpan.FromSeconds(0.1));
        Assert(vm.ShowContinueButton && engine.Phase == GamePhase.RoundEnd, "manual next-round control appears");
        Assert(vm.ShowRoundSummary && vm.JoinedSeats.Count() == 4, "round summary includes all four players");
        Assert(vm.JoinedSeats.All(s => s.Cards.All(c => c.Card is not null) && s.RoundResultDisplay.Contains("+")), "round summary contains every hand and award");
        engine.Tick(TimeSpan.FromMinutes(5));
        Assert(engine.Phase == GamePhase.RoundEnd, "manual results remain on screen until Continue");
        Render(game, "round-result.png");
        vm.HandleKeyDown(Key.Enter);
        Assert(engine.Phase == GamePhase.GameOver && vm.InitialsEntries.Count == 4, "game-over standings and four initials entries");
        foreach (var entry in vm.InitialsEntries) entry.Initials = "ACE";
        Render(game, "finish.png");
        Render(game, "finish-compact.png", 960, 640);
        vm.ExitGame();
        Assert(engine.Phase == GamePhase.Attract && engine.ActiveSeatIndex == 0, "return to menu resets active player");
        Assert(File.Exists(Path.Combine(data, "highscores.json")), "scores saved on return to menu");
        Assert(vm.HighScores.Count == 4, "all four high scores retained");
        var fourPlayerBoard = vm.SelectedLeaderboard!;
        vm.SelectedLeaderboard = vm.LeaderboardChoices.First(c => c.Key == "legacy");
        Assert(vm.HighScores.Count == 0, "new records are excluded from the legacy leaderboard");
        vm.SelectedLeaderboard = fourPlayerBoard;
        Assert(vm.HighScores.Count == 4, "saved records remain accessible under matching race rules");
        vm.HandleKeyDown(Key.Space);
        Assert(engine.Phase == GamePhase.Picking && engine.Seats.Count(s => s.Status == SeatStatus.Joined) == 1, "Space starts solo replay");
        Render(game, "solo.png");
        Render(game, "compact.png", 960, 600);
        for (int card = 0; card < 5; card++) vm.HandleKeyDown(Key.D1);
        engine.Tick(TimeSpan.FromSeconds(0.1)); engine.Tick(TimeSpan.FromSeconds(0.1)); vm.HandleKeyDown(Key.Enter);
        vm.InitialsEntries[0].Initials = "NEW";
        engine.Tick(TimeSpan.FromSeconds(settings.GameOverTimeoutSeconds + 1));
        Assert(vm.HighScores.Any(e => e.Initials == "NEW"), "results timeout saves entered initials");
        vm.HandleKeyDown(Key.D1);
        Render(game, "solo-default.png");
        settings.FinishDistance = 48000;
        var track = Find<RaceTrack>(game)!;
        track.FinishDistance = settings.FinishDistance;
        foreach (var seat in vm.SeatViewModels) { seat.TotalScore = 48000; seat.TrackScore = 48000; }
        Render(game, "finish-distance.png");
        Assert(track.FinishDistance == 48000, "track supports configured finish distance");
        vm.Stop();
        var referenceSettings = new GameSettings
        {
            SoundEnabled = false, MaxRounds = 9, FinishDistance = 150000,
            PickingTimeoutSeconds = 0, TurnDelaySeconds = 0.01,
            RacingDurationSeconds = 0.01, AutoAdvanceEnabled = false
        };
        vm = new MainViewModel(referenceSettings, Path.Combine(data, "reference-scoring"));
        timer = (DispatcherTimer)typeof(MainViewModel).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
        timer.Stop();
        engine = (GameEngine)typeof(MainViewModel).GetField("_engine", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
        game = new RacePokerGameView { DataContext = vm };
        vm.PlayerButtonPressed(0);
        var photoHand = new[]
        {
            new Card(Rank.Two, Suit.Diamonds), new Card(Rank.Five, Suit.Diamonds),
            new Card(Rank.Two, Suit.Clubs), new Card(Rank.Five, Suit.Clubs), new Card(Rank.Four, Suit.Spades)
        };
        for (int round = 1; round <= 9; round++)
        {
            engine.Seats[0].SelectionCards = photoHand.ToArray();
            for (int card = 0; card < 5; card++) engine.PickCard(0, card);
            Assert(engine.Seats[0].RoundPoints == 6700, $"photographed two pair pays 6,700 in round {round}");
            Assert(vm.HandPointsDisplay == "6,700", $"hand-points display matches payout in round {round}");
            engine.Tick(TimeSpan.FromSeconds(0.1)); engine.Tick(TimeSpan.FromSeconds(0.1));
            if (round < 9) engine.PressContinue(0);
        }
        Assert(engine.Seats[0].TotalScore == 60300, "nine photographed hands total 60,300 without bonuses");
        var deployedChart = PointTableService.Load(Path.Combine(AppContext.BaseDirectory, "Config", "pointTable.json"));
        Assert(deployedChart.GetPoints(HandEvaluator.Evaluate(photoHand)) == 6700, "deployed point chart matches photographed hand");
        Render(game, "reference-scoring-round9.png");
        vm.Stop();
        var editable = new GameSettings();
        var locked = new SettingsViewModel(editable, rulesEditable: false)
        {
            MaxRounds = 1, FinishDistance = 48000, UntimedPicking = true, FirstToFinish = true, SumHighCards = true, CardSwayDegrees = 0
        };
        locked.Apply();
        Assert(editable.MaxRounds == 10 && editable.FinishDistance == 150000 && editable.PickingTimeoutSeconds == 30 && editable.FinishRule == FinishRule.EndOfRound && editable.HighCardRule == HighCardRule.HighestRank, "race rule changes are blocked during an active game");
        Assert(editable.CardSwayDegrees == 0, "presentation settings remain editable during a game");
        var options = new SettingsViewModel(editable) { FinishDistance = 48000, UntimedPicking = true, FirstToFinish = true, SumHighCards = true };
        options.Apply();
        Assert(editable.FinishDistance == 48000 && editable.PickingTimeoutSeconds == 0 && editable.FinishRule == FinishRule.FirstToFinish && editable.HighCardRule == HighCardRule.SumOfRanks, "new race controls apply from the menu");
        var settingsWindow = new SettingsWindow { DataContext = options };
        Render((FrameworkElement)settingsWindow.Content, "settings.png", 640, 760);
        var soundType = typeof(MainViewModel).Assembly.GetType("RacePoker.Wpf.Services.SoundService")!;
        foreach (var name in new[] { "card_pick", "hand_complete", "racing", "round_end", "game_win", "game_over" })
        {
            var wave = (byte[])soundType.GetMethod("CreateSound", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { name })!;
            Assert(System.Text.Encoding.ASCII.GetString(wave, 0, 4) == "RIFF" && BitConverter.ToInt32(wave, 4) + 8 == wave.Length && wave.Skip(44).Any(b => b != 0), $"{name} generates a valid, non-silent WAV cue");
        }
        shell.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
        Assert(!((RotateTransform)shell.FindName("CardRotate1")).HasAnimatedProperties, "unloading the lobby releases its animations");
        var sprintSettings = new GameSettings { SoundEnabled = false, FinishDistance = 1000, FinishRule = FinishRule.FirstToFinish, PickingTimeoutSeconds = 0, TurnDelaySeconds = 0.01 };
        var sprint = new MainViewModel(sprintSettings, Path.Combine(data, "sprint"));
        ((DispatcherTimer)typeof(MainViewModel).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(sprint)!).Stop();
        var sprintEngine = (GameEngine)typeof(MainViewModel).GetField("_engine", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(sprint)!;
        var sprintView = new RacePokerGameView { DataContext = sprint };
        var host = new Window { Content = sprintView, Width = 1280, Height = 800, Left = -30000, Top = -30000, WindowStartupLocation = WindowStartupLocation.Manual, ShowActivated = false, ShowInTaskbar = false, WindowStyle = WindowStyle.None };
        host.Show();
        sprint.PlayerButtonPressed(1);
        sprintEngine.Seats[0].SelectionCards = photoHand.ToArray();
        for (int card = 0; card < 5; card++) sprintEngine.PickCard(0, card);
        sprintEngine.Tick(TimeSpan.FromSeconds(0.1));
        Assert(sprint.Phase == GamePhase.GameOver && sprint.WinnerDisplay == "PLAYER 1 WINS!" && sprintEngine.Seats[1].Hand.Length == 0, "first-to-finish presents winner before remaining turns");
        Assert(sprintView.IsLoaded && sprintView.IsVisible, "finish view loads through a real WPF window lifecycle");
        Render(sprintView, "first-to-finish.png");
        var celebration = Find<FinishCelebration>(sprintView)!;
        var celebrationClock = (System.Diagnostics.Stopwatch)typeof(FinishCelebration).GetField("_clock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(celebration)!;
        Assert(celebrationClock.IsRunning, "finish celebration starts when the results become visible");
        Assert(celebration.ActualWidth > 1000 && celebration.ActualHeight > 600, "celebration spans the finish screen");
        celebration.InvalidateVisual();
        Render(sprintView, "first-to-finish.png");
        sprint.Stop(); host.Close();
        Assert(!celebrationClock.IsRunning, "closing the finish view stops its animation");
        VerifySessionSafety(data);
        var bindingErrors = errors.ToString(); File.WriteAllText(Path.Combine(Output, "binding-errors.txt"), bindingErrors);
        Assert(string.IsNullOrWhiteSpace(bindingErrors), "no WPF binding errors");
        Console.WriteLine("Previews: " + Output);
    }

    sealed class ManualTime : TimeProvider
    {
        private long _timestamp;
        public DateTimeOffset WallTime = DateTimeOffset.UtcNow;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _timestamp;
        public override DateTimeOffset GetUtcNow() => WallTime;
        public void Advance(double seconds) => _timestamp += TimeSpan.FromSeconds(seconds).Ticks;
    }

    static void VerifySessionSafety(string data)
    {
        var clock = new ManualTime();
        var directory = Path.Combine(data, "session-safety");
        Directory.CreateDirectory(directory);
        var settings = new GameSettings { MaxSeats = 1, MaxRounds = 1, SoundEnabled = false, AutoAdvanceEnabled = false, TurnDelaySeconds = 0.01, RacingDurationSeconds = 0.01 };
        var vm = new MainViewModel(settings, directory, clock);
        ((DispatcherTimer)typeof(MainViewModel).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!).Stop();
        var engine = (GameEngine)typeof(MainViewModel).GetField("_engine", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
        var host = new MainWindow(vm) { Left = -30000, Top = -30000, WindowStartupLocation = WindowStartupLocation.Manual, ShowActivated = false, ShowInTaskbar = false };
        host.Show();
        vm.SetWindowActive(true);
        Assert(!vm.PlayerJoinCommand.CanExecute("3"), "unavailable player-count button is disabled");
        vm.HandleKeyDown(Key.D1, isRepeat: true);
        Assert(engine.Phase == GamePhase.Attract, "held-key repeats cannot start a race");
        var source = PresentationSource.FromVisual(host)!;
        UIElement menu = host;
        void SendKey(Key key)
        {
            var args = new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
            menu.RaiseEvent(args);
            Assert(args.Handled, $"window routes {key} as a game shortcut");
        }
        void Tick(double seconds)
        {
            clock.Advance(seconds);
            typeof(MainViewModel).GetMethod("OnTimerTick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, new object?[] { null, EventArgs.Empty });
        }
        SendKey(Key.D1);
        Assert(engine.Phase == GamePhase.Picking, "window input starts a supported race");
        host.UpdateLayout();
        menu = FindAll<Button>(host).First(b => b.Content?.ToString() == "Menu");
        SendKey(Key.D1);
        vm.HandleKeyDown(Key.D1, isRepeat: true); vm.HandleKeyDown(Key.F1, isRepeat: true);
        Assert(engine.Seats[0].Hand.Length == 1, "held selection keys cannot make additional picks");
        Tick(1);
        var remaining = vm.TimerRemainingFraction;
        clock.WallTime = clock.WallTime.AddDays(3);
        Tick(0);
        Assert(vm.TimerRemainingFraction == remaining, "wall-clock changes do not change picking time");
        vm.SetWindowActive(false);
        Tick(300);
        vm.HandleKeyDown(Key.D2);
        vm.ResumeGame();
        Assert(vm.IsPaused && engine.Seats[0].Hand.Length == 1 && vm.TimerRemainingFraction == remaining, "deactivation freezes time and input until the window is active");
        vm.SetWindowActive(true);
        Assert(vm.IsPaused, "returning to the window waits for explicit resume");
        var game = Find<RacePokerGameView>(host)!;
        Render(game, "paused.png");
        SendKey(Key.Enter);
        Tick(1);
        Assert(!vm.IsPaused && vm.TimerRemainingFraction < remaining, "resuming restarts the timer without charging away time");
        remaining = vm.TimerRemainingFraction;
        Tick(3600);
        Assert(vm.IsPaused && vm.TimerRemainingFraction == remaining, "sleep-sized dispatcher gaps pause rather than autofill a hand");
        SendKey(Key.P);
        Assert(!vm.IsPaused, "P resumes a paused race");
        SendKey(Key.P);
        Assert(vm.IsPaused, "P pauses an active race");
        SendKey(Key.P);
        for (int i = 0; i < 4; i++) vm.HandleKeyDown(Key.D1);
        engine.Tick(TimeSpan.FromSeconds(0.1)); engine.Tick(TimeSpan.FromSeconds(0.1));
        Assert(engine.Phase == GamePhase.RoundEnd, "limited-seat race reaches results");
        foreach (var key in new[] { Key.D2, Key.D3, Key.D4, Key.NumPad4 }) SendKey(key);
        Assert(engine.Phase == GamePhase.RoundEnd, "unavailable player numbers cannot crash or change round results");
        SendKey(Key.Enter);
        Assert(engine.Phase == GamePhase.GameOver, "Enter advances results through the window preview route");
        Tick(1);
        Assert(vm.ResultsCountdownDisplay.Contains("29s"), "results display remaining idle time");
        host.UpdateLayout();
        var text = FindAll<TextBox>(game).Single(t => t.DataContext is InitialsEntryViewModel);
        text.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, Environment.TickCount, null, text) { RoutedEvent = Keyboard.GotKeyboardFocusEvent });
        Assert(vm.IsEditingInitials && text.SelectionLength == text.Text.Length, "initials focus selects existing text and pauses the results timer");
        for (int second = 0; second < 40; second++) Tick(1);
        Assert(engine.Phase == GamePhase.GameOver && vm.ResultsCountdownDisplay.StartsWith("Timer paused"), "typing can outlast the results timeout safely");
        vm.ClearInitialsCommand.Execute(null);
        foreach (var c in new[] { "A", "C", "E", "X" })
        {
            var button = FindAll<Button>(game).Single(b => ReferenceEquals(b.Command, vm.EnterInitialsCommand) && b.Content?.ToString() == c);
            typeof(Button).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(button, null);
        }
        Assert(vm.ActiveInitialsEntry!.Initials == "ACE", "touch keypad enters initials and enforces three characters");
        vm.BackspaceInitialsCommand.Execute(null);
        Assert(vm.ActiveInitialsEntry.Initials == "AC", "touch backspace removes one initial");
        vm.EnterInitialsCommand.Execute("E");
        var enterInitials = new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, Key.Enter) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        text.RaiseEvent(enterInitials);
        Assert(enterInitials.Handled && engine.Phase == GamePhase.GameOver && !vm.IsEditingInitials, "Enter in an initials field finishes editing without leaving results");
        Tick(1);
        Assert(!vm.IsEditingInitials && vm.ResultsCountdownDisplay.Contains("29s"), "Done restarts the visible results countdown");
        host.Width = 960; host.Height = 640; host.UpdateLayout();
        Render((FrameworkElement)host.Content, "touch-initials.png", 960, 640);
        var lastKey = FindAll<Button>(game).Single(b => ReferenceEquals(b.Command, vm.EnterInitialsCommand) && b.Content?.ToString() == "9");
        var keyBounds = lastKey.TransformToAncestor((FrameworkElement)host.Content).TransformBounds(new Rect(0, 0, lastKey.ActualWidth, lastKey.ActualHeight));
        Assert(keyBounds.Height >= 44, "keypad touch targets remain at least 44 pixels tall at minimum window size");
        Assert(keyBounds.Right <= ((FrameworkElement)host.Content).ActualWidth && keyBounds.Bottom <= ((FrameworkElement)host.Content).ActualHeight, "entire touch keypad fits the minimum window size");
        host.Width = 1280; host.Height = 800; host.UpdateLayout();
        var scorePath = Path.Combine(directory, "highscores.json");
        File.WriteAllText(scorePath, "[]");
        using (var locked = new FileStream(scorePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            host.Close();
            Assert(host.IsVisible && vm.ShowCloseFailure && vm.HasPendingSaves, "failed score save prevents silent loss on window close");
            Assert(engine.HighScores.Entries.Single().Initials == "ACE", "failed save retains entered initials in memory");
            Render((FrameworkElement)host.Content, "save-retry.png");
        }
        vm.RetrySaveCommand.Execute(null);
        Assert(!vm.HasPendingSaves && !vm.ShowCloseFailure, "Retry save clears the pending state after access is restored");
        var reloaded = new HighScoreService(scorePath); reloaded.Load();
        Assert(reloaded.Entries.Single().Initials == "ACE", "retry writes the retained score exactly once");
        var settingsPath = Path.Combine(directory, "settings.json");
        new SettingsService(settingsPath).Save(settings);
        using (var locked = new FileStream(settingsPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            settings.LogoGlowBlur = 15;
            typeof(MainViewModel).GetMethod("SettingsApplied", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, null);
            Assert(vm.HasPendingSaves, "failed settings save is also tracked for retry");
        }
        for (int second = 0; second < 5; second++) Tick(1);
        Assert(!vm.HasPendingSaves && new SettingsService(settingsPath).Load().LogoGlowBlur == 15, "automatic retry persists pending settings after access returns");
        using (var locked = new FileStream(settingsPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            settings.LogoGlowBlur = 20;
            typeof(MainViewModel).GetMethod("SettingsApplied", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, null);
        }
        host.Close();
        Assert(!host.IsVisible && new SettingsService(settingsPath).Load().LogoGlowBlur == 20, "window close retries pending settings before stopping");
        var editedSettings = new GameSettings();
        var options = new SettingsViewModel(editedSettings) { FinishDistanceText = "48000" };
        Assert(options.Apply() && editedSettings.FinishDistance == 48000, "typed finish distance applies precisely");
        options.FinishDistanceText = "nonsense";
        Assert(!options.Apply() && options.FinishDistanceError.Length > 0 && editedSettings.FinishDistance == 48000, "invalid numeric entry cannot overwrite valid settings");
        options.FinishDistanceText = "1000001";
        Assert(!options.Apply(), "out-of-range numeric entry is rejected");
        options.DecreaseDistanceCommand.Execute(null);
        Assert(options.FinishDistance == 47000 && options.FinishDistanceError.Length == 0, "touch stepper restores valid numeric entry");
    }

    static IEnumerable<T> FindAll<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is T result) yield return result;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            foreach (var child in FindAll<T>(VisualTreeHelper.GetChild(parent, i))) yield return child;
    }

    static T? Find<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is T target) return target;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var result = Find<T>(VisualTreeHelper.GetChild(parent, i)); if (result is not null) return result; }
        return null;
    }
    static void Render(FrameworkElement view, string name, int width = 1280, int height = 800)
    {
        view.Measure(new Size(width, height)); view.Arrange(new Rect(0, 0, width, height)); view.UpdateLayout();
        view.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        System.Threading.Thread.Sleep(250);
        view.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
        view.Measure(new Size(width, height)); view.Arrange(new Rect(0, 0, width, height)); view.UpdateLayout();
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(view);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(Output, name)); encoder.Save(stream);
    }
}
