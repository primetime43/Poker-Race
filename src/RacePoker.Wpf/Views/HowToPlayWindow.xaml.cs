using System.Windows;

namespace RacePoker.Wpf.Views;

public partial class HowToPlayWindow : Window
{
    public HowToPlayWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
