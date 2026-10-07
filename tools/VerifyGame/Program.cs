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
        for (int seat = 1; seat < 4; seat++)
        {
            for (int card = 0; card < 5; card++) vm.HandleKeyDown(Key.D1);
            engine.Tick(TimeSpan.FromSeconds(0.1));
        }
        Assert(engine.Phase == GamePhase.Racing, "all four hands advance to race standings");
        engine.Tick(TimeSpan.FromSeconds(0.1));
        Assert(vm.ShowContinueButton && engine.Phase == GamePhase.RoundEnd, "manual next-round control appears");
        Render(game, "round-result.png");
        vm.HandleKeyDown(Key.Enter);
        Assert(engine.Phase == GamePhase.GameOver && vm.InitialsEntries.Count == 4, "game-over standings and four initials entries");
        foreach (var entry in vm.InitialsEntries) entry.Initials = "ACE";
        Render(game, "finish.png");
        vm.ExitGame();
        Assert(engine.Phase == GamePhase.Attract && engine.ActiveSeatIndex == 0, "return to menu resets active player");
        Assert(File.Exists(Path.Combine(data, "highscores.json")), "scores saved on return to menu");
        Assert(vm.HighScores.Count == 4, "all four high scores retained");
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
