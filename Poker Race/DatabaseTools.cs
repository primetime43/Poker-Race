using System;
using System.Net;
using System.Windows;

namespace Poker_Race
{
    class DatabaseTools
    {
        public void connectToDatabase(string playerName, string score)
        {
            try
            {
                //HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://primetime43.x10host.com/PokerRace/updateTable.php" + "?name=" + playerName + "&score=" + score);

                /*HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader input = new StreamReader(response.GetResponseStream());*/

                var webClient = new WebClient();
                //Console.WriteLine("http://primetime43.x10host.com/PokerRace/updateTable.php" + "?name=" + playerName + "&score=" + score);
                webClient.DownloadString("http://primetime43.x10host.com/PokerRace/updateTable.php" + "?name=" + playerName + "&score=" + score);
            }
            catch (Exception Except)
            {
                MessageBox.Show(Except.ToString());
            }
        }

    }
}
