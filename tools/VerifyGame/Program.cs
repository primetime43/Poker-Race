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
        typeof(MainViewModel).GetField("_lastTick", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(vm, DateTime.UtcNow);
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
        Render((FrameworkElement)settingsWindow.Content, "settings.png", 480, 640);
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
        var bindingErrors = errors.ToString(); File.WriteAllText(Path.Combine(Output, "binding-errors.txt"), bindingErrors);
        Assert(string.IsNullOrWhiteSpace(bindingErrors), "no WPF binding errors");
        Console.WriteLine("Previews: " + Output);
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
