using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using RacePoker.Wpf.ViewModels;

namespace RacePoker.Wpf.Views;

public partial class ShellView : UserControl
{
    private MainViewModel? _observed;
    public ShellView()
    {
        InitializeComponent();
        Loaded += (_, _) => Observe();
        DataContextChanged += (_, _) => { if (IsLoaded) Observe(); };
        Unloaded += (_, _) => { Detach(); ApplyAnimations(0, 0); };
    }

    private void Observe()
    {
        Detach();
        _observed = DataContext as MainViewModel;
        if (_observed is not null) _observed.PropertyChanged += OnSettingsChanged;
        ApplyAnimations(_observed?.CardSwayDegrees ?? 10, _observed?.LogoGlowBlur ?? 30);
    }

    private void Detach()
    {
        if (_observed is not null) _observed.PropertyChanged -= OnSettingsChanged;
        _observed = null;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.CardSwayDegrees) or nameof(MainViewModel.LogoGlowBlur))
            ApplyAnimations(_observed?.CardSwayDegrees ?? 0, _observed?.LogoGlowBlur ?? 0);
    }

    private void ApplyAnimations(double sway, double glow)
    {
        Sway(CardRotate1, -15, sway, 2.5);
        Sway(CardRotate2, 0, sway, 3);
        Sway(CardRotate3, 15, sway, 3.5);
        LogoGlow.BeginAnimation(DropShadowEffect.OpacityProperty, null);
        LogoGlow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, null);
        LogoGlow.Opacity = 0;
        LogoGlow.BlurRadius = 0;
        if (glow <= 0) return;
        LogoGlow.BeginAnimation(DropShadowEffect.OpacityProperty, Pulse(0.2, 0.8, 1.2));
        LogoGlow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, Pulse(glow / 3, glow, 1.2));
    }

    private static void Sway(RotateTransform transform, double angle, double sway, double seconds)
    {
        transform.BeginAnimation(RotateTransform.AngleProperty, null);
        transform.Angle = angle;
        if (sway > 0) transform.BeginAnimation(RotateTransform.AngleProperty, Pulse(angle - sway, angle + sway, seconds));
    }

    private static DoubleAnimation Pulse(double from, double to, double seconds)
        => new(from, to, TimeSpan.FromSeconds(seconds)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
}
