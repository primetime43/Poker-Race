using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RacePoker.Wpf.ViewModels;

namespace RacePoker.Wpf.Views;

public partial class MainWindow : Window
{
    private bool _isFullscreen;
    private WindowState _previousWindowState;
    public MainWindow() { InitializeComponent(); }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            if (_isFullscreen)
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;
                WindowState = _previousWindowState;
            }
            else
            {
                _previousWindowState = WindowState;
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;
            }
            _isFullscreen = !_isFullscreen;
            e.Handled = true;
            return;
        }
        // Initials and text fields own their keyboard input.
        if (e.OriginalSource is TextBox || Keyboard.Modifiers != ModifierKeys.None) return;
        if (DataContext is MainViewModel vm)
        {
            vm.HandleKeyDown(e.Key);
            e.Handled = e.Key is Key.D1 or Key.D2 or Key.D3 or Key.D4 or Key.D5
                or Key.NumPad1 or Key.NumPad2 or Key.NumPad3 or Key.NumPad4 or Key.NumPad5
                or Key.F1 or Key.F2 or Key.F3 or Key.F4 or Key.F5 or Key.Enter or Key.Space or Key.Escape;
        }
    }
    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.Stop();
        base.OnClosed(e);
    }
}
