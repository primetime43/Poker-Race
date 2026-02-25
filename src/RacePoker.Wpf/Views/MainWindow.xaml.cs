using System.Windows;
using System.Windows.Input;
using RacePoker.Wpf.ViewModels;

namespace RacePoker.Wpf.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.HandleKeyDown(e.Key);
    }
}
