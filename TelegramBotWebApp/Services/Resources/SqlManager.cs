using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBotWebApp.Services.Resources
{
    internal class SqlManager
    {

        string sqlConnectstring;
        SqlConnection sqlConnection = new();

        public SqlManager()
        {
            sqlConnectstring = @"Server=tcp:sqlserverforbot.database.windows.net,1433;"
                              +@"Initial Catalog=telegram-bot-sql-server;Persist Security Info=False;"
                              +@"User ID=myadmin;Password={your_password};MultipleActiveResultSets=False;"
                              +@"Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            sqlConnection.ConnectionString = sqlConnectstring;
        }
        private void Connect()
        {
            sqlConnection.Open();
        }

        private void Disconnect()
        {
            sqlConnection.Close();
        }

        public Ship GetShipFromDbByName(string ShipName)
        {
            Connect();
            SqlCommand selectCmd = new($"SELECT * FROM ShipsTable WHERE ShipName LIKE '%{ShipName}%'");
            selectCmd.Connection = sqlConnection;
            SqlDataAdapter adapter = new(selectCmd);
            Ship _ship = new();
            DataTable table = new();
            adapter.Fill(table);
            _ship.ShipName = table.Rows[0][1].ToString();
            _ship.ShipCode = table.Rows[0][2].ToString();
            Disconnect();
            return _ship;
        }

        public Port GetPortFromDbByName(string portName)
        {
            Connect();
            SqlCommand selectCmd = new($"SELECT * FROM PortsTable WHERE PortName LIKE '%{portName}%'");
            selectCmd.Connection = sqlConnection;
            SqlDataAdapter sqlAdapter = new SqlDataAdapter(selectCmd);
            Port _port = new();
            DataTable table = new();
            sqlAdapter.Fill(table);
            _port.locationName = table.Rows[0][1].ToString();
            _port.countryName = table.Rows[0][2].ToString();
            Disconnect();
            return _port;
        }

        public string GetEmoji(string countryName)
        {
            Connect();
            SqlCommand selectCmd = new($"SELECT Emoji FROM CountriesTable WHERE CountryName = '{countryName}'");
            selectCmd.Connection = sqlConnection;
            SqlDataAdapter adapter = new(selectCmd);
            DataTable table = new();
            adapter.Fill(table);
            Disconnect();
            if (table.Rows[0][0].ToString() == null)
            {
                return "";
            }
            return table.Rows[0][0].ToString();

        }

        #region Populate Tables
        /*public void AddCountries(List<EmojiClass> emojiList)
        {
            Connect();
            foreach (var emoji in emojiList)
            {
                SqlCommand checkCmd = new("SELECT COUNT (*) FROM CountriesTable WHERE CountryName=@name");
                checkCmd.Parameters.Add("@name", SqlDbType.NVarChar);
                checkCmd.Parameters["@name"].Value = emoji.Name;
                checkCmd.Connection = sqlConnection;
                Object obj = checkCmd.ExecuteScalar();

                if ((int)obj != 0)
                    continue;

                string query = $"INSERT INTO CountriesTable (CountryName, Emoji) VALUES (@name, @emoji);";
                SqlCommand insertCmd = new();
                insertCmd.CommandType = CommandType.Text;
                insertCmd.CommandText = query;
                insertCmd.Parameters.Add("@name", SqlDbType.NVarChar);
                insertCmd.Parameters.Add("@emoji", SqlDbType.NVarChar);
                insertCmd.Parameters["@name"].Value = emoji.Name;
                insertCmd.Parameters["@emoji"].Value = emoji.Emoji;
                insertCmd.Connection = sqlConnection;
                insertCmd.ExecuteNonQuery();
            }
            Disconnect();
        }

        public void AddVessels(VesselsManager vesselsManager)
        {
            foreach (var vessel in vesselsManager.ships)
            {
                SqlCommand checkCmd = new("SELECT COUNT (*) FROM ShipsTable WHERE ShipName=@ShipName");
                checkCmd.Parameters.Add("@ShipName", SqlDbType.NVarChar);
                checkCmd.Parameters["@ShipName"].Value = vessel.ShipName;
                checkCmd.Connection = sqlConnection;
                Object obj = checkCmd.ExecuteScalar();

                if ((int)obj != 0)
                    continue;

                string query = $"INSERT INTO ShipsTable (ShipName, ShipCode) VALUES (@ShipName, @ShipCode);";
                SqlCommand insertCmd = new();
                insertCmd.CommandType = CommandType.Text;
                insertCmd.CommandText = query;
                insertCmd.Parameters.Add("@ShipName", SqlDbType.NVarChar);
                insertCmd.Parameters.Add("@ShipCode", SqlDbType.NVarChar);
                insertCmd.Parameters["@ShipName"].Value = vessel.ShipName;
                insertCmd.Parameters["@ShipCode"].Value = vessel.ShipCode;
                insertCmd.Connection = sqlConnection;
                insertCmd.ExecuteNonQuery();
            }
        }*/
        #endregion
    }
}
