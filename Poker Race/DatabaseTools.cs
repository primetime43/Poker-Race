using System;
using System.Net.Http;
using System.Windows;

namespace Poker_Race
{
    class DatabaseTools
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public async void connectToDatabase(string playerName, string score)
        {
            try
            {
                await httpClient.GetStringAsync("http://primetime43.x10host.com/PokerRace/updateTable.php" + "?name=" + Uri.EscapeDataString(playerName) + "&score=" + Uri.EscapeDataString(score));
            }
            catch (Exception Except)
            {
                MessageBox.Show(Except.ToString());
            }
        }

    }
}
