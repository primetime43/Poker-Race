using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using RacePoker.Wpf.ViewModels;

namespace RacePoker.Wpf.Controls;

/// <summary>A track measured against the actual available width and configured finish score.</summary>
public sealed class RaceTrack : FrameworkElement
{
    public static readonly DependencyProperty SeatsProperty = DependencyProperty.Register(
        nameof(Seats), typeof(IEnumerable<SeatViewModel>), typeof(RaceTrack),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSeatsChanged));
    public static readonly DependencyProperty FinishDistanceProperty = DependencyProperty.Register(
        nameof(FinishDistance), typeof(int), typeof(RaceTrack),
        new FrameworkPropertyMetadata(150000, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty IsDemoProperty = DependencyProperty.Register(
        nameof(IsDemo), typeof(bool), typeof(RaceTrack),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty UseSymbolProperty = DependencyProperty.Register(
        nameof(UseSymbol), typeof(bool), typeof(RaceTrack),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable<SeatViewModel>? Seats { get => (IEnumerable<SeatViewModel>?)GetValue(SeatsProperty); set => SetValue(SeatsProperty, value); }
    public int FinishDistance { get => (int)GetValue(FinishDistanceProperty); set => SetValue(FinishDistanceProperty, value); }
    public bool IsDemo { get => (bool)GetValue(IsDemoProperty); set => SetValue(IsDemoProperty, value); }
    public bool UseSymbol { get => (bool)GetValue(UseSymbolProperty); set => SetValue(UseSymbolProperty, value); }

    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly List<SeatViewModel> _observed = new();
    private double _lastFrame;
    private static readonly Color[] Colors = { Color.FromRgb(247, 133, 117), Color.FromRgb(117, 187, 247), Color.FromRgb(114, 230, 194), Color.FromRgb(244, 207, 117) };

    public RaceTrack()
    {
        Loaded += (_, _) => { ObserveSeats(); CompositionTarget.Rendering += OnFrame; };
        Unloaded += (_, _) => { CompositionTarget.Rendering -= OnFrame; UnobserveSeats(); };
        IsVisibleChanged += (_, _) => InvalidateVisual();
    }

    private static void OnSeatsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var track = (RaceTrack)d;
        if (e.OldValue is INotifyCollectionChanged oldCollection) oldCollection.CollectionChanged -= track.OnCollectionChanged;
        track.UnobserveSeats();
        if (e.NewValue is INotifyCollectionChanged collection) collection.CollectionChanged += track.OnCollectionChanged;
        track.ObserveSeats();
    }
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) { UnobserveSeats(); ObserveSeats(); InvalidateVisual(); }
    private void ObserveSeats()
    {
        if (_observed.Count > 0 || Seats is null) return;
        foreach (var seat in Seats) { seat.PropertyChanged += OnSeatChanged; _observed.Add(seat); }
    }
    private void UnobserveSeats() { foreach (var seat in _observed) seat.PropertyChanged -= OnSeatChanged; _observed.Clear(); }
    private void OnSeatChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SeatViewModel.TrackScore) or nameof(SeatViewModel.IsActiveSeat) or nameof(SeatViewModel.Status) or nameof(SeatViewModel.TotalScore) or nameof(SeatViewModel.IsTurnTransitioning)) InvalidateVisual();
    }
    private void OnFrame(object? sender, EventArgs e)
    {
        var seconds = _clock.Elapsed.TotalSeconds;
        if (IsVisible && seconds - _lastFrame >= 1.0 / 30 && (IsDemo || _observed.Any(s => s.IsTurnTransitioning))) { _lastFrame = seconds; InvalidateVisual(); }
    }
    private void Text(DrawingContext dc, string text, double x, double y, double size, Brush brush, bool bold = false)
    {
        var formatted = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, bold ? FontWeights.SemiBold : FontWeights.Normal, FontStretches.Normal),
            size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(formatted, new Point(x, y));
    }
    private static SolidColorBrush Brush(string color) => new((Color)ColorConverter.ConvertFromString(color));

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double w = ActualWidth, h = ActualHeight;
        if (w < 240 || h < 80) return;
        var seats = Seats?.Where(s => s.IsJoined).ToArray() ?? Array.Empty<SeatViewModel>();
        int count = IsDemo ? 4 : Math.Max(1, seats.Length);
        const double labelWidth = 116, top = 25, bottom = 29;
        double laneHeight = (h - top - bottom) / count;
        double start = labelWidth + 8, finish = w - 26;
        double horseHeight = Math.Min(48, laneHeight - 8), horseWidth = horseHeight * 1.54;
        double travel = Math.Max(0, finish - start - horseWidth - 4);
        dc.DrawRoundedRectangle(Brush("#102C29"), null, new Rect(0, 0, w, h), 10, 10);
        dc.DrawRectangle(new LinearGradientBrush(Color.FromRgb(32, 91, 66), Color.FromRgb(21, 63, 49), 90), null, new Rect(labelWidth, top, w - labelWidth, h - top - bottom));
        // Mown grass and the white rails keep the visual language of the reference cabinet.
        for (double x = labelWidth; x < w; x += 42)
            dc.DrawRectangle(Brush("#0AFFFFFF"), null, new Rect(x, top, Math.Min(21, w - x), h - top - bottom));
        var rail = new Pen(Brush("#B7C9A9"), 2);
        dc.DrawLine(rail, new Point(labelWidth, top), new Point(w, top));
        dc.DrawLine(rail, new Point(labelWidth, h - bottom), new Point(w, h - bottom));
        for (double x = labelWidth; x < w; x += 23)
        {
            dc.DrawLine(rail, new Point(x, top), new Point(x, top + 7));
            dc.DrawLine(rail, new Point(x, h - bottom - 7), new Point(x, h - bottom));
        }
        Text(dc, IsDemo ? "THE PADDOCK" : "LIVE STANDINGS", 12, 5, 10, Brush("#A8C8BA"), true);
        Text(dc, "FINISH", finish - 24, 5, 10, Brush("#F4CF75"), true);
        double seconds = _clock.Elapsed.TotalSeconds;
        for (int i = 0; i < count; i++)
        {
            var seat = IsDemo ? null : seats.ElementAtOrDefault(i);
            int id = seat?.SeatId ?? i;
            var color = new SolidColorBrush(Colors[id % Colors.Length]);
            double y = top + i * laneHeight;
            bool active = seat?.IsActiveSeat == true;
            if (active) dc.DrawRectangle(Brush("#12FFFFFF"), null, new Rect(0, y, w, laneHeight));
            if (i > 0) dc.DrawLine(new Pen(Brush("#33799A83"), 1), new Point(labelWidth, y), new Point(w, y));
            dc.DrawEllipse(color, null, new Point(18, y + laneHeight / 2), 4, 4);
            Text(dc, $"PLAYER {id + 1}", 30, y + laneHeight / 2 - 16, 10, color, true);
            Text(dc, IsDemo ? "Ready to race" : (seat?.TotalScore ?? 0).ToString("N0"), 30, y + laneHeight / 2, 12, Brushes.White, true);
            double fraction = IsDemo ? 0.15 + 0.6 * ((Math.Sin(seconds * 0.24 + i * 0.8) + 1) / 2) : Math.Clamp((double)(seat?.TrackScore ?? 0) / Math.Max(1, FinishDistance), 0, 1);
            double x = start + travel * fraction;
            bool moving = IsDemo || (seat?.IsTurnTransitioning == true && seat.IsActiveSeat);
            double bob = moving ? Math.Sin(seconds * 18 + i) * 1.5 : 0;
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(75, Colors[id % 4].R, Colors[id % 4].G, Colors[id % 4].B)), 3), new Point(start, y + laneHeight - 8), new Point(x + horseWidth / 2, y + laneHeight - 8));
            dc.DrawEllipse(Brush("#40000000"), null, new Point(x + horseWidth / 2, y + laneHeight - 7), horseWidth * 0.4, 3);
            double horseY = y + (laneHeight - horseHeight) / 2 + bob;
            if (UseSymbol)
                Text(dc, "♞", x + 8, horseY - 8, horseHeight, color, true);
            else
            {
                DrawHorse(dc, x, horseY, horseWidth, horseHeight, color, id, seconds * 12 + i, moving);
            }
        }
        // The checkerboard sits at the end of the same distance used for horse movement.
        for (double y = top; y < h - bottom; y += 8)
            for (int col = 0; col < 2; col++)
                dc.DrawRectangle(((int)((y - top) / 8) + col) % 2 == 0 ? Brush("#D9E4CE") : Brush("#173329"), null, new Rect(finish + col * 8, y, 8, Math.Min(8, h - bottom - y)));
        for (int i = 0; i <= 4; i++)
        {
            double x = start + (finish - start) * i / 4;
            int score = (int)Math.Round(Math.Max(1, FinishDistance) * i / 4.0);
            Text(dc, score == 0 ? "START" : score.ToString("N0"), Math.Clamp(x - 18, start, w - 62), h - 21, 10, Brush("#BDD4C6"));
        }
    }
    private static void DrawHorse(DrawingContext dc, double x, double y, double width, double height, Brush saddle, int id, double gait, bool moving)
    {
        dc.PushTransform(new TranslateTransform(x, y));
        dc.PushTransform(new ScaleTransform(width / 100, height / 66));
        var coats = new[] { "#7B4430", "#343237", "#B4773D", "#786B59" };
        var coat = Brush(coats[id % coats.Length]);
        var dark = Brush("#211E25");
        // Four jointed legs alternate between extension and a tucked recovery stroke.
        for (int leg = 0; leg < 4; leg++)
        {
            double hipX = leg < 2 ? 30 : 67;
            double phase = gait + (leg % 2) * Math.PI + (leg < 2 ? 1.1 : 0);
            double stride = moving ? Math.Sin(phase) * 16 : (leg % 2 == 0 ? -5 : 5);
            var hip = new Point(hipX, 39);
            var knee = new Point(hipX + stride * 0.5, 50 - (moving ? Math.Max(0, Math.Cos(phase)) * 7 : 0));
            var hoof = new Point(hipX + stride, 62 - (moving ? Math.Max(0, Math.Cos(phase)) * 13 : 0));
            var pen = new Pen(leg % 2 == 0 ? dark : coat, 4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            dc.DrawLine(pen, hip, knee);
            dc.DrawLine(pen, knee, hoof);
            dc.DrawLine(new Pen(dark, 4), hoof, new Point(hoof.X + 4, hoof.Y));
        }
        var tail = new StreamGeometry();
        using (var g = tail.Open())
        {
            g.BeginFigure(new Point(24, 28), false, false);
            g.BezierTo(new Point(10, 24), new Point(13, moving ? 15 + Math.Sin(gait) * 5 : 37), new Point(2, 31), true, false);
        }
        dc.DrawGeometry(null, new Pen(dark, 6), tail);
        dc.DrawEllipse(coat, null, new Point(48, 33), 28, 13);
        var neck = new StreamGeometry();
        using (var g = neck.Open())
        {
            g.BeginFigure(new Point(62, 31), true, true);
            g.LineTo(new Point(74, 10), true, false);
            g.LineTo(new Point(84, 13), true, false);
            g.LineTo(new Point(76, 39), true, false);
        }
        dc.DrawGeometry(coat, null, neck);
        dc.DrawLine(new Pen(dark, 4), new Point(70, 23), new Point(76, 10));
        dc.DrawEllipse(coat, null, new Point(85, 15), 12, 6);
        dc.DrawLine(new Pen(coat, 4), new Point(79, 11), new Point(78, 5));
        dc.DrawEllipse(Brush("#F4E7CF"), null, new Point(92, 17), 4, 4);
        dc.DrawEllipse(dark, null, new Point(85, 13), 1.5, 1.5);
        dc.DrawLine(new Pen(saddle, 2), new Point(88, 9), new Point(88, 21));
        dc.DrawRoundedRectangle(saddle, null, new Rect(39, 24, 22, 17), 3, 3);
        dc.DrawLine(new Pen(dark, 2), new Point(85, 19), new Point(66, 20));
        // The jockey crouches over the saddle, moving with the horse's stride.
        dc.DrawLine(new Pen(saddle, 7), new Point(48, 23), new Point(62, 13));
        dc.DrawEllipse(Brush("#EAC5A2"), null, new Point(66, 9), 4, 4);
        dc.DrawEllipse(saddle, null, new Point(65, 6), 5, 3);
        dc.DrawLine(new Pen(Brush("#ECE7D8"), 3), new Point(60, 15), new Point(68, 20));
        dc.DrawLine(new Pen(Brush("#ECE7D8"), 4), new Point(48, 24), new Point(56, 32));
        dc.DrawLine(new Pen(dark, 3), new Point(56, 32), new Point(51, 38));
        var number = new FormattedText((id + 1).ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), 12, dark, 1);
        dc.DrawText(number, new Point(45, 24));
        dc.Pop();
        dc.Pop();
    }

}
