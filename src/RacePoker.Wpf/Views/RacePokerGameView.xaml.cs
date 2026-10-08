using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RacePoker.Wpf.ViewModels;

namespace RacePoker.Wpf.Views;

public partial class RacePokerGameView : UserControl
{
    public RacePokerGameView() { InitializeComponent(); }
    private void Initials_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox text && text.DataContext is InitialsEntryViewModel entry && DataContext is MainViewModel vm)
        {
            vm.BeginInitialsEntry(entry);
            text.SelectAll();
        }
    }
    private void Initials_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox text && text.DataContext is InitialsEntryViewModel entry && DataContext is MainViewModel vm)
        {
            vm.BeginInitialsEntry(entry);
            if (!text.IsKeyboardFocusWithin) { e.Handled = true; text.Focus(); text.SelectAll(); }
        }
    }
    private void InitialsRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border { Child: Grid grid, DataContext: InitialsEntryViewModel entry } && DataContext is MainViewModel vm)
        {
            vm.BeginInitialsEntry(entry);
            var text = grid.Children.OfType<TextBox>().FirstOrDefault();
            if (text is not null && !text.IsKeyboardFocusWithin) { text.Focus(); text.SelectAll(); }
        }
    }
    private void DoneInitials_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.CompleteInitialsEntry();
        Keyboard.ClearFocus();
    }
}
