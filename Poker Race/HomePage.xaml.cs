using System;
using System.Windows;
using System.Windows.Controls;

namespace Poker_Race
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        public static string username;
        public HomePage()
        {
            InitializeComponent();
        }

        private void startGame(object sender, RoutedEventArgs e)
        {
            if (usernameTextBox.Text.Equals("Enter Your Name") || usernameTextBox.Text.Length < 3)
            {
                GameMainPage.AutoClosingMessageBox.Show("You must enter your name (min length 3)", "Error", 1600);
                usernameTextBox.Text = "Enter Your Name";
            }
            else
            {
                username = usernameTextBox.Text;
                this.NavigationService.Navigate(new GameMainPage());
            }
        }


        private void UsernameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            usernameTextBox.Clear();
        }

        private void UsernameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(usernameTextBox.Text))
                usernameTextBox.Text = "Enter Your Name";
        }
    }
}
