using System.Windows;
using RacePoker.Wpf.ViewModels;

namespace RacePoker.Wpf.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        MaxHeight = Math.Max(400, SystemParameters.WorkArea.Height - 24);
        Height = Math.Min(760, MaxHeight);
        Width = Math.Min(640, SystemParameters.WorkArea.Width - 24);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && !vm.Apply()) return;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
