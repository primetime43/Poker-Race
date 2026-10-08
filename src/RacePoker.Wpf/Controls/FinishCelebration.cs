using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace RacePoker.Wpf.Controls;

/// <summary>A short, bounded burst on entering the finish screen.</summary>
public sealed class FinishCelebration : FrameworkElement
{
    private readonly Stopwatch _clock = new();
    private double _lastFrame;
    private static readonly Brush[] Colors = { Brushes.Gold, Brushes.MediumAquamarine, Brushes.Coral, Brushes.LightSkyBlue };
    public FinishCelebration()
    {
        ClipToBounds = true;
        Loaded += (_, _) => Start();
        IsVisibleChanged += (_, _) => { if (IsVisible) Start(); else Stop(); };
        Unloaded += (_, _) => Stop();
    }
    private void Start()
    {
        Stop();
        if (!IsVisible) return;
        _lastFrame = 0;
        _clock.Restart();
        CompositionTarget.Rendering += OnFrame;
        InvalidateVisual();
    }
    private void Stop() { CompositionTarget.Rendering -= OnFrame; _clock.Stop(); }
    private void OnFrame(object? sender, EventArgs e)
    {
        if (_clock.Elapsed.TotalSeconds >= 3.5) { Stop(); InvalidateVisual(); return; }
        if (_clock.Elapsed.TotalSeconds - _lastFrame < 1.0 / 30) return;
        _lastFrame = _clock.Elapsed.TotalSeconds;
        InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        double t = _clock.Elapsed.TotalSeconds;
        if (t >= 3.5 || ActualWidth <= 0) return;
        dc.PushOpacity(Math.Clamp((3.5 - t) / 1.2, 0, 1));
        for (int i = 0; i < 90; i++)
        {
            double seed = i * 2.399963;
            double x = ActualWidth * (0.5 + 0.46 * Math.Sin(seed)) + Math.Sin(t * 3 + seed) * 25;
            double y = -25 - i % 9 * 12 + t * (150 + i % 7 * 26);
            dc.PushTransform(new RotateTransform(seed * 90 + t * 150, x, y));
            dc.DrawRectangle(Colors[i % Colors.Length], null, new Rect(x, y, 5, 11));
            dc.Pop();
        }
        dc.Pop();
    }
}
