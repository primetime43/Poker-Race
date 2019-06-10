using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Poker_Race
{
    class DatabaseTools
    {
        public void connectToDatabase()
        {
            try
            {
                MySqlConnection connection = new MySqlConnection("datasource=localhost;port=3306;username=;password=");
                connection.Open();
                if (connection.State == ConnectionState.Open)
                {
                    MessageBox.Show("Connected");
                }
                else
                {
                    MessageBox.Show("Not Connected");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    }
}
