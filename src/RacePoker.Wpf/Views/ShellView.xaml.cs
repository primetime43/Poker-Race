using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using RacePoker.Wpf.ViewModels;

namespace RacePoker.Wpf.Views;

public partial class ShellView : UserControl
{
    public ShellView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        double swayDegrees = 10;
        double glowBlur = 30;

        if (DataContext is MainViewModel vm)
        {
            swayDegrees = vm.CardSwayDegrees;
            glowBlur = vm.LogoGlowBlur;
        }

        StartCardSwayAnimation(swayDegrees);
        StartLogoGlowAnimation(glowBlur);
    }

    private void StartCardSwayAnimation(double degrees)
    {
        if (degrees <= 0) return;

        var sb = new Storyboard();

        // Card 1: base angle -15
        var anim1 = new DoubleAnimation
        {
            From = -15 - degrees,
            To = -15 + degrees,
            Duration = TimeSpan.FromSeconds(2.5),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        Storyboard.SetTargetName(anim1, "CardRotate1");
        Storyboard.SetTargetProperty(anim1, new PropertyPath("Angle"));
        sb.Children.Add(anim1);

        // Card 2: base angle 0
        var anim2 = new DoubleAnimation
        {
            From = -degrees,
            To = degrees,
            Duration = TimeSpan.FromSeconds(3.0),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        Storyboard.SetTargetName(anim2, "CardRotate2");
        Storyboard.SetTargetProperty(anim2, new PropertyPath("Angle"));
        sb.Children.Add(anim2);

        // Card 3: base angle 15
        var anim3 = new DoubleAnimation
        {
            From = 15 - degrees,
            To = 15 + degrees,
            Duration = TimeSpan.FromSeconds(3.5),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        Storyboard.SetTargetName(anim3, "CardRotate3");
        Storyboard.SetTargetProperty(anim3, new PropertyPath("Angle"));
        sb.Children.Add(anim3);

        sb.Begin(this);
    }

    private void StartLogoGlowAnimation(double maxBlur)
    {
        if (maxBlur <= 0) return;

        var sb = new Storyboard();

        var opacityAnim = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(1.2),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        Storyboard.SetTargetName(opacityAnim, "LogoGlow");
        Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));
        sb.Children.Add(opacityAnim);

        var blurAnim = new DoubleAnimation
        {
            From = maxBlur / 3.0,
            To = maxBlur,
            Duration = TimeSpan.FromSeconds(1.2),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        Storyboard.SetTargetName(blurAnim, "LogoGlow");
        Storyboard.SetTargetProperty(blurAnim, new PropertyPath("BlurRadius"));
        sb.Children.Add(blurAnim);

        sb.Begin(this);
    }
}
