using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RacePoker.Wpf.ViewModels;

namespace RacePoker.Wpf.Views;

public partial class MainWindow : Window
{
    private bool _isFullscreen;
    private bool _allowUnsavedClose;
    private WindowState _previousWindowState;
    public MainWindow() : this(new MainViewModel()) { }
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Activated += (_, _) => viewModel.SetWindowActive(WindowState != WindowState.Minimized);
        Deactivated += (_, _) => viewModel.SetWindowActive(false);
        StateChanged += (_, _) => { if (WindowState == WindowState.Minimized) viewModel.SetWindowActive(false); };
        viewModel.CloseWithoutSavingRequested += CloseWithoutSaving;
    }

    private void CloseWithoutSaving() { _allowUnsavedClose = true; Close(); }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            e.Handled = true;
            if (e.IsRepeat) return;
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
            return;
        }
        if (DataContext is not MainViewModel vm || Keyboard.Modifiers != ModifierKeys.None) return;
        if (vm.ShowCloseFailure) return;
        // Text entry owns letters and numbers. Enter finishes initials rather than leaving results.
        if (Keyboard.FocusedElement is TextBox text || e.OriginalSource is TextBox)
        {
            if (e.Key is Key.Enter or Key.Escape && vm.IsEditingInitials)
            {
                e.Handled = true;
                if (!e.IsRepeat) { vm.CompleteInitialsEntry(); Keyboard.ClearFocus(); }
            }
            return;
        }
        bool gameKey = e.Key is Key.D1 or Key.D2 or Key.D3 or Key.D4 or Key.D5
            or Key.NumPad1 or Key.NumPad2 or Key.NumPad3 or Key.NumPad4 or Key.NumPad5
            or Key.F1 or Key.F2 or Key.F3 or Key.F4 or Key.F5 or Key.Enter or Key.Space or Key.Escape or Key.P;
        if (!gameKey) return;
        // Preview handling prevents a previously focused button from consuming a game shortcut.
        e.Handled = true;
        vm.HandleKeyDown(e.Key, e.IsRepeat);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowUnsavedClose && DataContext is MainViewModel vm && !vm.PrepareToClose()) e.Cancel = true;
        base.OnClosing(e);
    }
    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CloseWithoutSavingRequested -= CloseWithoutSaving;
            vm.Stop();
        }
        base.OnClosed(e);
    }
}
